using Microsoft.Data.SqlClient;
using TradingMcp.Audit;
using TradingMcp.Auth;
using TradingMcp.Repository;

namespace TradingMcp;

internal static class McpServerApplication
{
    private const string UserSecretsHint = "See docs/02-run-server-and-inspector.md for the user-secrets commands.";

    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration;
        var connectionString = RequireConnectionString(configuration);
        var useEntra = UsesEntraAuthentication(configuration);

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

        // Anonymous, so a hosting platform can check the server is up. It doesn't touch the database.
        application.MapGet("/health", () => Results.Ok(new { status = "ok" }));

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
