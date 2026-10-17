using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingMcp.Auth;

namespace XTests.McpServer;

public class When_Entra_Authentication_Is_Registered
{
    [Theory]
    [InlineData("Entra:TenantId")]
    [InlineData("Entra:ClientId")]
    public void it_should_require_tenant_and_client_ids(string missingKey)
    {
        IConfiguration configuration;
        IServiceCollection services;

        configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Entra:TenantId"] = missingKey == "Entra:TenantId" ? null : "tenant-id",
                ["Entra:ClientId"] = missingKey == "Entra:ClientId" ? null : "client-id"
            })
            .Build();
        services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddEntraAuth(configuration));

        Assert.Contains(missingKey, exception.Message);
    }

    [Fact]
    public async Task it_should_authorize_users_with_the_required_scope_or_application_role()
    {
        IConfiguration configuration;
        ServiceProvider serviceProvider;
        IAuthorizationService authorizationService;
        ClaimsPrincipal scopeUser;
        ClaimsPrincipal roleUser;
        ClaimsPrincipal missingPermissionUser;

        configuration = CreateConfiguration();
        serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddEntraAuth(configuration)
            .BuildServiceProvider();
        authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        scopeUser = CreateAuthenticatedUser(new Claim("scp", "openid mcp.read profile"));
        roleUser = CreateAuthenticatedUser(new Claim("roles", "MCP.READ"));
        missingPermissionUser = CreateAuthenticatedUser(new Claim("scp", "openid"));

        Assert.True((await authorizationService.AuthorizeAsync(
            scopeUser, resource: null, EntraAuthExtensions.ReadPolicy)).Succeeded);
        Assert.True((await authorizationService.AuthorizeAsync(
            roleUser, resource: null, EntraAuthExtensions.ReadPolicy)).Succeeded);
        Assert.False((await authorizationService.AuthorizeAsync(
            missingPermissionUser, resource: null, EntraAuthExtensions.ReadPolicy)).Succeeded);
    }

    [Fact]
    public async Task it_should_not_authorize_anonymous_users_even_with_the_required_scope()
    {
        IConfiguration configuration;
        ServiceProvider serviceProvider;
        IAuthorizationService authorizationService;
        ClaimsPrincipal anonymousUser;

        configuration = CreateConfiguration();
        serviceProvider = new ServiceCollection()
            .AddLogging()
            .AddEntraAuth(configuration)
            .BuildServiceProvider();
        authorizationService = serviceProvider.GetRequiredService<IAuthorizationService>();
        anonymousUser = new ClaimsPrincipal(new ClaimsIdentity([new Claim("scp", "mcp.read")]));

        Assert.False((await authorizationService.AuthorizeAsync(
            anonymousUser, resource: null, EntraAuthExtensions.ReadPolicy)).Succeeded);
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Entra:TenantId"] = "tenant-id",
                ["Entra:ClientId"] = "client-id"
            })
            .Build();
    }

    private static ClaimsPrincipal CreateAuthenticatedUser(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }
}
