using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingMcp.Audit;
using TradingMcp.Auth;
using TradingMcp.Repository;

namespace TradingMcp;

internal static class McpServerApplication
{
    internal static WebApplication Build(string[] args)
    {
        WebApplicationBuilder builder;
        string connectionString;
        bool useEntra;
        WebApplication application;

        builder = WebApplication.CreateBuilder(args);
        connectionString = GetConnectionString(builder.Configuration);
        useEntra = UsesEntraAuthentication(builder.Configuration);
        AddServices(builder, connectionString, useEntra);
        application = builder.Build();
        ConfigureApplication(application, useEntra);

        return application;
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        string? connectionString;

        connectionString = configuration.GetConnectionString("Trading");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:Trading is not configured.");
        }

        return connectionString;
    }

    private static bool UsesEntraAuthentication(IConfiguration configuration)
    {
        string authMode;

        authMode = configuration["Auth:Mode"] ?? "StaticTokens";
        if (authMode.Equals("Entra", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (authMode.Equals("StaticTokens", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        throw new InvalidOperationException(
            $"Unknown Auth:Mode '{authMode}'. Use 'StaticTokens' or 'Entra'.");
    }

    private static void AddServices(
        WebApplicationBuilder builder,
        string connectionString,
        bool useEntra)
    {
        AddDatabaseServices(builder.Services, connectionString);
        AddAuthenticationServices(builder.Services, builder.Configuration, useEntra);
        CallerRateLimiter.AddServices(builder.Services);
        AddMcpServices(builder.Services);
    }

    private static void AddDatabaseServices(IServiceCollection services,     string connectionString)
    {
    services.AddSingleton<Func<SqlConnection>>(_ => () => new SqlConnection(connectionString));
    services.AddSingleton<ITradingRepository, TradingRepository>();
    services.AddHttpContextAccessor();
    services.AddSingleton<IAuditService, AuditService>();
    }

    private static void AddAuthenticationServices(
        IServiceCollection services,
        IConfiguration configuration,
        bool useEntra)
    {
        if (useEntra)
        {
            services.AddEntraAuth(configuration);
        }
    }

    private static void AddMcpServices(IServiceCollection services)
    {
        services
            .AddMcpServer()
            .WithHttpTransport(http => http.Stateless = true)
            .WithToolsFromAssembly();
    }

    private static void ConfigureApplication(WebApplication application, bool useEntra)
    {
        application.Logger.LogInformation(
            "Authentication mode: {Mode}",
            useEntra ? "Entra" : "StaticTokens");

        MapHealthEndpoint(application);
        ConfigureAuthenticationMiddleware(application, useEntra);
        application.UseRateLimiter();
        MapMcpEndpoint(application, useEntra);
    }

    private static void MapHealthEndpoint(WebApplication application)
    {
        application.MapGet("/health", () => Results.Ok(new { status = "ok" }));
    }

    private static void ConfigureAuthenticationMiddleware(WebApplication application, bool useEntra)
    {
        if (useEntra)
        {
            application.UseAuthentication();
            application.UseAuthorization();
        }
        else
        {
            application.UseMiddleware<BearerTokenMiddleware>();
        }
    }

    private static void MapMcpEndpoint(WebApplication application, bool useEntra)
    {
        var mcp = application.MapMcp("/mcp");
        if (useEntra)
        {
            mcp.RequireAuthorization(EntraAuthExtensions.ReadPolicy);
        }
    }
}
