using Microsoft.Data.SqlClient;
using TradingMcp.Audit;
using TradingMcp.Auth;
using TradingMcp.Repository;

namespace TradingMcp;

internal static class McpServerApplication
{
    private const string UserSecretsHint = "See docs/02-run-server-and-inspector.md for the user-secrets commands.";
    internal const int MaxRequestBodyBytes = 64 * 1024;

    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;
        var envFilePath = UseEnvFileInDevelopment(builder);
        var connectionString = RequireConnectionString(configuration);
        var useEntra = UsesEntraAuthentication(configuration);

        // MCP tool calls are a few hundred bytes. Reject anything bigger than 64 KB before it's even parsed,
        // instead of Kestrel's default of 30 MB.
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = MaxRequestBodyBytes);

        builder.Services.AddSingleton<Func<SqlConnection>>(_ => () => new SqlConnection(connectionString));
        builder.Services.AddSingleton<ITradingRepository, TradingRepository>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IAuditService, AuditService>();
        if (useEntra)
        {
            builder.Services.AddEntraAuth(configuration);
        }
        else
        {
            builder.Services.AddSingleton(BearerTokenMiddleware.LoadTokens(configuration));
        }

        CallerRateLimiter.AddServices(builder.Services);
        builder.Services.AddMcpServer().WithHttpTransport(http => http.Stateless = true).WithToolsFromAssembly();

        var application = builder.Build();
        application.Logger.LogInformation("Authentication mode: {Mode}", useEntra ? "Entra" : "StaticTokens");
        if (envFilePath is not null)
        {
            application.Logger.LogInformation("Using settings from {Path}", envFilePath);
        }

        // Anonymous, so a hosting platform can check the server is up. It doesn't touch the database.
        application.MapGet("/health", () => Results.Ok(new { status = "ok" }));

        // First, so it sees the final status code of every request that authentication or the rate limiter rejects.
        application.UseMiddleware<RejectedRequestLogger>();

        if (useEntra)
        {
            application.UseAuthentication();
            application.UseAuthorization();
        }
        else
        {
            application.UseMiddleware<BearerTokenMiddleware>();
        }

        // After authentication, so each caller gets their own limit.
        application.UseRateLimiter();

        var mcp = application.MapMcp("/mcp");
        if (useEntra)
        {
            mcp.RequireAuthorization(EntraAuthExtensions.ReadPolicy);
        }

        return application;
    }

    // Local walkthrough only: settings that aren't configured come from the Docker setup's .env file.
    // Anything set in user-secrets or the environment always wins.
    private static string? UseEnvFileInDevelopment(WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
        {
            return null;
        }

        var values = EnvFile.TryRead(builder.Environment.ContentRootPath, out var path);
        if (values is null)
        {
            return null;
        }

        var configuration = builder.Configuration;
        var used = false;
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("Trading"))
            && values.GetValueOrDefault("MCP_READER_PASSWORD") is { Length: > 0 } password)
        {
            configuration["ConnectionStrings:Trading"] = EnvFile.BuildReaderConnectionString(password);
            used = true;
        }

        if (!configuration.GetSection("Mcp:Tokens").GetChildren().Any()
            && values.GetValueOrDefault("MCP_TOKEN") is { Length: > 0 } token)
        {
            configuration["Mcp:Tokens:local"] = token;
            used = true;
        }

        return used ? path : null;
    }

    private static string RequireConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Trading");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ServerConfigurationException($"ConnectionStrings:Trading isn't set. {UserSecretsHint}");
        }

        return connectionString;
    }

    private static bool UsesEntraAuthentication(IConfiguration configuration)
    {
        var authMode = configuration["Auth:Mode"] ?? "StaticTokens";
        if (authMode.Equals("Entra", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (authMode.Equals("StaticTokens", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        throw new ServerConfigurationException($"Unknown Auth:Mode '{authMode}'. Use 'StaticTokens' or 'Entra'.");
    }
}
