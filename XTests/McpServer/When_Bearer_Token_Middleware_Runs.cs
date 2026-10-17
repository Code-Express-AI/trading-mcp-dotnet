using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using TradingMcp.Auth;

namespace XTests.McpServer;

public class When_Bearer_Token_Middleware_Runs
{
    private const string ConfiguredToken = "test-token-with-more-than-thirty-two-characters";

    [Fact]
    public async Task it_should_allow_health_requests_without_a_token()
    {
        int nextCallCount;
        BearerTokenMiddleware middleware;
        DefaultHttpContext context;

        nextCallCount = 0;
        middleware = CreateMiddleware(() => nextCallCount++);
        context = new DefaultHttpContext();
        context.Request.Path = "/health/ready";

        await middleware.InvokeAsync(context);

        Assert.Equal(1, nextCallCount);
        Assert.NotEqual(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Basic abc")]
    [InlineData("Bearer wrong-token")]
    public async Task it_should_reject_missing_or_invalid_authorization_headers(string authorizationHeader)
    {
        int nextCallCount;
        BearerTokenMiddleware middleware;
        DefaultHttpContext context;

        nextCallCount = 0;
        middleware = CreateMiddleware(() => nextCallCount++);
        context = new DefaultHttpContext();
        context.Request.Path = "/mcp";
        context.Request.Headers.Authorization = authorizationHeader;

        await middleware.InvokeAsync(context);

        Assert.Equal(0, nextCallCount);
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("Bearer", context.Response.Headers.WWWAuthenticate);
    }

    [Fact]
    public async Task it_should_authenticate_the_matching_named_token()
    {
        int nextCallCount;
        BearerTokenMiddleware middleware;
        DefaultHttpContext context;

        nextCallCount = 0;
        middleware = CreateMiddleware(() => nextCallCount++);
        context = new DefaultHttpContext();
        context.Request.Path = "/mcp";
        context.Request.Headers.Authorization = $"bEaReR  {ConfiguredToken} ";

        await middleware.InvokeAsync(context);

        Assert.Equal(1, nextCallCount);
        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.Equal("inspector", context.User.Identity?.Name);
    }

    [Fact]
    public void it_should_require_at_least_one_non_empty_configured_token()
    {
        Assert.Throws<InvalidOperationException>(() => CreateMiddleware(() => { }, []));
        Assert.Throws<InvalidOperationException>(() => CreateMiddleware(
            () => { },
            [new KeyValuePair<string, string?>("inspector", "")]));
    }

    [Fact]
    public void it_should_reject_configured_tokens_that_are_too_short()
    {
        Assert.Throws<InvalidOperationException>(() => CreateMiddleware(
            () => { },
            [new KeyValuePair<string, string?>("inspector", "short-token")]));
    }

    private static BearerTokenMiddleware CreateMiddleware(
        Action next,
        IEnumerable<KeyValuePair<string, string?>>? tokens = null)
    {
        IConfiguration configuration;
        RequestDelegate requestDelegate;

        configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(tokens ??
            [
                new KeyValuePair<string, string?>("Mcp:Tokens:inspector", ConfiguredToken)
            ])
            .Build();
        requestDelegate = _ =>
        {
            next();
            return Task.CompletedTask;
        };

        return new BearerTokenMiddleware(requestDelegate, configuration);
    }
}
