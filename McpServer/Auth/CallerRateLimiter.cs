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
        });
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
