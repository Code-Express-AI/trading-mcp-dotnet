using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace TradingMcp.Auth;

internal static class CallerRateLimiter
{
    private const int PermitLimit = 120;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    internal static void AddServices(IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = GetRateLimiter();
            options.OnRejected = AddRetryAfterHeader;
        });
    }

    internal static ValueTask AddRetryAfterHeader(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (!context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            return ValueTask.CompletedTask;
        }

        var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
        context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        return ValueTask.CompletedTask;
    }

    internal static PartitionedRateLimiter<HttpContext> GetRateLimiter()
    {
        return PartitionedRateLimiter.Create<HttpContext, string>(
            context => RateLimitPartition.GetFixedWindowLimiter(
                context.User.GetCallerId(),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = PermitLimit,
                    Window = Window
                }));
    }
}
