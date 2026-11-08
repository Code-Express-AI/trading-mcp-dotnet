using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TradingMcp.Auth;

namespace XTests.McpServer;

public class When_Origin_Is_Checked
{
    private int _nextCallCount;

    private OriginCheckMiddleware CreateMiddleware(params string[] allowedOrigins) =>
        new(_ => { _nextCallCount++; return Task.CompletedTask; },
            new ConfigurationBuilder()
                .AddInMemoryCollection(allowedOrigins.Select((origin, i) =>
                    new KeyValuePair<string, string?>($"Mcp:AllowedOrigins:{i}", origin)))
                .Build(),
            NullLogger<OriginCheckMiddleware>.Instance);

    private static DefaultHttpContext CreateRequest(string? origin)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/mcp";
        if (origin is not null)
        {
            context.Request.Headers.Origin = origin;
        }

        return context;
    }

    [Fact]
    public async Task it_should_allow_a_request_without_an_origin()
    {
        var context = CreateRequest(origin: null);

        await CreateMiddleware().InvokeAsync(context);

        Assert.Equal(1, _nextCallCount);
    }

    [Theory]
    [InlineData("https://evil.example")]
    [InlineData("http://localhost:6274")]
    [InlineData("null")]
    public async Task it_should_refuse_an_origin_that_is_not_listed(string origin)
    {
        var context = CreateRequest(origin);

        await CreateMiddleware().InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        Assert.Equal(0, _nextCallCount);
    }

    [Theory]
    [InlineData("https://trusted.example")]
    [InlineData("https://trusted.example/")]
    public async Task it_should_allow_a_listed_origin(string allowedOrigin)
    {
        var context = CreateRequest("https://trusted.example");

        await CreateMiddleware(allowedOrigin).InvokeAsync(context);

        Assert.Equal(1, _nextCallCount);
    }
}
