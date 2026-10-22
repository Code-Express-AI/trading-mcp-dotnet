using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using TradingMcp;
using TradingMcp.Auth;

namespace XTests.McpServer;

public class When_Bearer_Token_Middleware_Is_Run
{
    private const string ConfiguredToken = "test-token-with-more-than-thirty-two-characters";

    private int _nextCallCount;

    private static IConfiguration CreateConfiguration(params (string Name, string? Value)[] tokens) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(tokens.Select(token => new KeyValuePair<string, string?>($"Mcp:Tokens:{token.Name}", token.Value)))
            .Build();

    private BearerTokenMiddleware CreateMiddleware() =>
        new(_ => { _nextCallCount++; return Task.CompletedTask; },
            BearerTokenMiddleware.LoadTokens(CreateConfiguration(("inspector", ConfiguredToken))));

    private static DefaultHttpContext CreateRequest(string path, string? authorization = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Headers.Authorization = authorization;
        return context;
    }

    [Fact]
    public async Task it_should_allow_health_requests_without_a_token()
    {
        var context = CreateRequest("/health/ready");

        await CreateMiddleware().InvokeAsync(context);

        Assert.Equal(1, _nextCallCount);
        Assert.NotEqual(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Basic abc")]
    [InlineData("Bearer wrong-token")]
    public async Task it_should_reject_missing_or_invalid_authorization_headers(string authorization)
    {
        var context = CreateRequest("/mcp", authorization);

        await CreateMiddleware().InvokeAsync(context);

        Assert.Equal(0, _nextCallCount);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("Bearer", context.Response.Headers.WWWAuthenticate);
    }

    [Fact]
    public async Task it_should_authenticate_the_matching_named_token()
    {
        var context = CreateRequest("/mcp", $"bEaReR  {ConfiguredToken} ");

        await CreateMiddleware().InvokeAsync(context);

        Assert.Equal(1, _nextCallCount);
        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.Equal("inspector", context.User.Identity?.Name);
    }

    [Fact]
    public void it_should_explain_how_to_set_a_token_when_none_are_configured()
    {
        var exception = Assert.Throws<ServerConfigurationException>(() => BearerTokenMiddleware.LoadTokens(CreateConfiguration()));

        Assert.StartsWith("No bearer tokens are set.", exception.Message);
        Assert.Contains("dotnet user-secrets set \"Mcp:Tokens:inspector\"", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short-token")]
    public void it_should_name_the_token_that_is_empty_or_too_short(string value)
    {
        var exception = Assert.Throws<ServerConfigurationException>(() =>
            BearerTokenMiddleware.LoadTokens(CreateConfiguration(("inspector", ConfiguredToken), ("desktop", value))));

        Assert.StartsWith("Mcp:Tokens:desktop must be at least 32 characters.", exception.Message);
    }
}
