using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingMcp;
using TradingMcp.Auth;

namespace XTests.McpServer;

public class When_Entra_Authentication_Is_Registered
{
    private static IConfiguration CreateConfiguration(string? tenantId = "tenant-id", string? clientId = "client-id") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Entra:TenantId"] = tenantId, ["Entra:ClientId"] = clientId })
            .Build();

    private static IAuthorizationService CreateAuthorizationService() =>
        new ServiceCollection().AddLogging().AddEntraAuth(CreateConfiguration()).BuildServiceProvider()
            .GetRequiredService<IAuthorizationService>();

    private static ClaimsPrincipal CreateAuthenticatedUser(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Test"));

    private static async Task<bool> IsAuthorized(ClaimsPrincipal user) =>
        (await CreateAuthorizationService().AuthorizeAsync(user, resource: null, EntraAuthExtensions.ReadPolicy)).Succeeded;

    [Theory]
    [InlineData("Entra:TenantId")]
    [InlineData("Entra:ClientId")]
    public void it_should_require_tenant_and_client_ids(string missingKey)
    {
        var configuration = missingKey == "Entra:TenantId" ? CreateConfiguration(tenantId: null) : CreateConfiguration(clientId: null);

        var exception = Assert.Throws<ServerConfigurationException>(() => new ServiceCollection().AddEntraAuth(configuration));

        Assert.Equal($"{missingKey} must be set when Auth:Mode is 'Entra'.", exception.Message);
    }

    [Fact]
    public async Task it_should_authorize_users_with_the_required_scope_or_application_role()
    {
        Assert.True(await IsAuthorized(CreateAuthenticatedUser(new Claim("scp", "openid mcp.read profile"))));
        Assert.True(await IsAuthorized(CreateAuthenticatedUser(new Claim("roles", "MCP.READ"))));
        Assert.False(await IsAuthorized(CreateAuthenticatedUser(new Claim("scp", "openid"))));
    }

    [Fact]
    public async Task it_should_not_authorize_anonymous_users_even_with_the_required_scope()
    {
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity([new Claim("scp", "mcp.read")]));

        Assert.False(await IsAuthorized(anonymousUser));
    }
}
