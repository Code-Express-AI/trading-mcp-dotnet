namespace TradingMcp.Auth;

/// <summary>
/// Refuses any request sent by a web page on another site. Browsers send an Origin header; MCP hosts such as VS Code,
/// Claude Code and MCP Inspector's proxy don't. The MCP specification (Streamable HTTP, "Security Warning")
/// requires servers to validate Origin and answer an invalid one with 403, to stop DNS rebinding.
/// Host filtering and the bearer token already stop that attack; this is the check the specification asks for.
/// Requests without an Origin header pass. Mcp:AllowedOrigins lists any browser origins we trust (none by default).
/// </summary>
internal sealed class OriginCheckMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<OriginCheckMiddleware> log)
{
    // Browsers send the origin without a trailing slash, so one in the setting would never match.
    private readonly HashSet<string> _allowedOrigins = new(
        (configuration.GetSection("Mcp:AllowedOrigins").Get<string[]>() ?? []).Select(origin => origin.TrimEnd('/')),
        StringComparer.OrdinalIgnoreCase);

    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (origin.Length > 0 && !_allowedOrigins.Contains(origin))
        {
            log.LogInformation("The origin '{Origin}' is not allowed.", origin);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await next(context);
    }
}
