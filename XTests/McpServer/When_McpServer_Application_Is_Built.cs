using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TradingMcp;

namespace XTests.McpServer;

public class When_McpServer_Application_Is_Built
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void it_should_reject_a_missing_database_connection_string(string connectionString)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            McpServerApplication.Build(
                [$"--ConnectionStrings:Trading={connectionString}"]));

        Assert.Equal("ConnectionStrings:Trading is not configured.", exception.Message);
    }

    [Fact]
    public void it_should_reject_an_unknown_authentication_mode()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            McpServerApplication.Build(
            [
                "--ConnectionStrings:Trading=Server=localhost;Database=TradingDemo;",
                "--Auth:Mode=Unknown"
            ]));

        Assert.Equal("Unknown Auth:Mode 'Unknown'. Use 'StaticTokens' or 'Entra'.", exception.Message);
    }

    [Fact]
    public async Task it_should_build_the_static_token_application_with_mcp_and_rate_limiting_services()
    {
        await using var application = McpServerApplication.Build(
        [
            "--ConnectionStrings:Trading=Server=localhost;Database=TradingDemo;"
        ]);

        Assert.NotNull(application.Services.GetRequiredService<TradingMcp.Repository.ITradingRepository>());
        Assert.NotNull(application.Services.GetRequiredService<TradingMcp.Audit.IAuditService>());
        Assert.NotNull(application.Services.GetRequiredService<IOptions<RateLimiterOptions>>().Value.GlobalLimiter);
    }

    [Fact]
    public async Task it_should_register_entra_authentication_and_protect_the_mcp_endpoint()
    {
        await using var application = McpServerApplication.Build(
        [
            "--ConnectionStrings:Trading=Server=localhost;Database=TradingDemo;",
            "--Auth:Mode=Entra",
            "--Entra:TenantId=test-tenant",
            "--Entra:ClientId=test-client"
        ]);
        var schemeProvider = application.Services.GetRequiredService<IAuthenticationSchemeProvider>();
        var policyProvider = application.Services.GetRequiredService<IAuthorizationPolicyProvider>();
        var policy = await policyProvider.GetPolicyAsync("McpRead");

        Assert.NotNull(await schemeProvider.GetDefaultAuthenticateSchemeAsync());
        Assert.NotNull(policy);
    }
}
