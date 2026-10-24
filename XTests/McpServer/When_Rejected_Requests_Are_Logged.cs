using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TradingMcp.Auth;

namespace XTests.McpServer;

public class When_Rejected_Requests_Are_Logged
{
    private const string Token = "test-token-with-more-than-thirty-two-characters";

    private readonly RecordingLogger _log = new();

    private async Task<DefaultHttpContext> RunAsync(int statusCode, string? authorization = null, string? callerName = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/mcp";
        context.Request.Headers.Authorization = authorization;
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");
        if (callerName is not null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, callerName)], authenticationType: "Bearer"));
        }

        var middleware = new RejectedRequestLogger(next => { next.Response.StatusCode = statusCode; return Task.CompletedTask; }, _log);
        await middleware.InvokeAsync(context);
        return context;
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status401Unauthorized)]
    [InlineData(StatusCodes.Status403Forbidden)]
    [InlineData(StatusCodes.Status429TooManyRequests)]
    public async Task it_should_log_a_warning_for_each_rejected_request(int statusCode)
    {
        await RunAsync(statusCode);

        var entry = Assert.Single(_log.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal($"Rejected POST /mcp with {statusCode} for caller anonymous from 203.0.113.7 (Authorization header sent: False)", entry.Message);
    }

    [Fact]
    public async Task it_should_name_the_caller_that_was_rate_limited()
    {
        await RunAsync(StatusCodes.Status429TooManyRequests, $"Bearer {Token}", "inspector");

        Assert.Contains("for caller inspector", Assert.Single(_log.Entries).Message);
    }

    [Fact]
    public async Task it_should_never_log_the_supplied_token()
    {
        await RunAsync(StatusCodes.Status401Unauthorized, $"Bearer {Token}");

        var message = Assert.Single(_log.Entries).Message;
        Assert.DoesNotContain(Token, message);
        Assert.Contains("(Authorization header sent: True)", message);
    }

    [Theory]
    [InlineData(StatusCodes.Status200OK)]
    [InlineData(StatusCodes.Status404NotFound)]
    [InlineData(StatusCodes.Status500InternalServerError)]
    public async Task it_should_not_log_requests_that_were_not_rejected(int statusCode)
    {
        await RunAsync(statusCode);

        Assert.Empty(_log.Entries);
    }

    private sealed class RecordingLogger : ILogger<RejectedRequestLogger>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
