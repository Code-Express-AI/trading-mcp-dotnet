using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingMcp;
using TradingMcp.Audit;
using TradingMcp.Auth;
using TradingMcp.Repository;

namespace XTests.McpServer;

public class When_Mcp_Server_Application_Is_Built
{
    private const string ConnectionString = "--ConnectionStrings:Trading=Server=localhost;Database=TradingDemo;";
    private const string Token = "--Mcp:Tokens:inspector=test-token-with-more-than-thirty-two-characters";

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void it_should_explain_a_missing_connection_string(string connectionString)
    {
        var exception = Assert.Throws<ServerConfigurationException>(() =>
            McpServerApplication.Build([$"--ConnectionStrings:Trading={connectionString}", Token]));

        Assert.Equal(
            "ConnectionStrings:Trading isn't set. See docs/02-run-server-and-inspector.md for the user-secrets commands.",
            exception.Message);
    }

    [Fact]
    public void it_should_reject_an_unknown_authentication_mode()
    {
        var exception = Assert.Throws<ServerConfigurationException>(() =>
            McpServerApplication.Build([ConnectionString, Token, "--Auth:Mode=Unknown"]));

        Assert.Equal("Unknown Auth:Mode 'Unknown'. Use 'StaticTokens' or 'Entra'.", exception.Message);
    }

    [Fact]
    public void it_should_fail_while_building_when_no_bearer_tokens_are_configured()
    {
        var exception = Assert.Throws<ServerConfigurationException>(() => McpServerApplication.Build([ConnectionString]));

        Assert.StartsWith("No bearer tokens are set.", exception.Message);
    }

    [Fact]
    public async Task it_should_build_the_static_token_application_with_its_services()
    {
        await using var application = McpServerApplication.Build([ConnectionString, Token]);

        Assert.NotNull(application.Services.GetRequiredService<ITradingRepository>());
        Assert.NotNull(application.Services.GetRequiredService<IAuditService>());
        Assert.Single(application.Services.GetRequiredService<BearerToken[]>());
        Assert.NotNull(application.Services.GetRequiredService<IOptions<RateLimiterOptions>>().Value.GlobalLimiter);
    }

    [Fact]
    public async Task it_should_register_entra_authentication_and_protect_the_mcp_endpoint_without_needing_tokens()
    {
        await using var application = McpServerApplication.Build(
            [ConnectionString, "--Auth:Mode=Entra", "--Entra:TenantId=test-tenant", "--Entra:ClientId=test-client"]);
        var schemeProvider = application.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        var policyProvider = application.Services.GetRequiredService<IAuthorizationPolicyProvider>();

        Assert.NotNull(await schemeProvider.GetDefaultAuthenticateSchemeAsync());
        Assert.NotNull(await policyProvider.GetPolicyAsync(EntraAuthExtensions.ReadPolicy));
    }
}
