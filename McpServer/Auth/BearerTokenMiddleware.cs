using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace TradingMcp.Auth;

internal sealed record BearerToken(string Name, byte[] Hash);

/// <summary>
/// Minimal bearer-token authentication. Each client gets its own named token
/// (Mcp:Tokens:claude-desktop, Mcp:Tokens:inspector, ...), so the audit log shows WHICH client
/// made each call and one token can be revoked without touching the others.
///
/// This is the simple version, used while building the server. For production, validate JWTs from Microsoft Entra ID
/// with AddJwtBearer instead - you get expiry, rotation and per-user identity.
/// </summary>
internal sealed class BearerTokenMiddleware(RequestDelegate next, BearerToken[] tokens)
{
    private const int MinTokenLength = 32;

    private const string SetTokenHint =
        "From the repository root, run: dotnet user-secrets set \"Mcp:Tokens:inspector\" \"<random token of 32+ characters>\" --project McpServer";

    // Called while the app is built, so a missing or weak token stops the server before it starts.
    // Only hashes are kept in memory; comparison is on equal-length digests.
    public static BearerToken[] LoadTokens(IConfiguration configuration)
    {
        var configured = configuration.GetSection("Mcp:Tokens").GetChildren().ToList();
        if (configured.Count == 0)
        {
            throw new ServerConfigurationException($"No bearer tokens are set. {SetTokenHint}");
        }

        foreach (var token in configured)
        {
            if (string.IsNullOrEmpty(token.Value) || token.Value.Length < MinTokenLength)
            {
                throw new ServerConfigurationException(
                    $"Mcp:Tokens:{token.Key} must be at least {MinTokenLength} characters. {SetTokenHint}");
            }
        }

        return configured.Select(token => new BearerToken(token.Key, Hash(token.Value!))).ToArray();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Anonymous health probe for the hosting platform. Exposes nothing.
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var supplied = Hash(header[7..].Trim());
            foreach (var (name, hash) in tokens)
            {
                if (CryptographicOperations.FixedTimeEquals(supplied, hash))
                {
                    context.User = new ClaimsPrincipal(
                        new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], authenticationType: "Bearer"));
                    await next(context);
                    return;
                }
            }
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Bearer";
    }

    private static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
}
