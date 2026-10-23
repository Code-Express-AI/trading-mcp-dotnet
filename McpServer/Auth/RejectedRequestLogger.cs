namespace TradingMcp.Auth;

/// <summary>
/// Logs every request that authentication, authorization or the rate limiter turned away, so repeated token
/// guessing or flooding leaves a trace. These go to the app logs, not trading.AuditLog: anyone on the internet
/// can send a request with a bad token, and an append-only table must not fill up with their junk.
/// The supplied token is never logged.
/// </summary>
internal sealed class RejectedRequestLogger(RequestDelegate next, ILogger<RejectedRequestLogger> log)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.Response.StatusCode is not (StatusCodes.Status401Unauthorized
            or StatusCodes.Status403Forbidden
            or StatusCodes.Status429TooManyRequests))
        {
            return;
        }

        log.LogWarning(
            "Rejected {Method} {Path} with {StatusCode} for caller {Caller} from {RemoteIp} (Authorization header sent: {HasAuthorizationHeader})",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            context.User.GetCallerId(),
            context.Connection.RemoteIpAddress,
            context.Request.Headers.Authorization.Count > 0);
    }
}
