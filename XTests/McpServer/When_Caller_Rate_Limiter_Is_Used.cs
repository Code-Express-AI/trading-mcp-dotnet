using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using TradingMcp.Auth;

namespace XTests.McpServer;

public class When_Caller_Rate_Limiter_Is_Used
{
    [Fact]
    public async Task it_should_limit_each_authenticated_caller_independently()
    {
        using var limiter = CallerRateLimiter.GetRateLimiter();
        var firstCaller = CreateContext("inspector");
        var secondCaller = CreateContext("desktop");

        for (var request = 0; request < 120; request++)
        {
            using var lease = await limiter.AcquireAsync(firstCaller);
            Assert.True(lease.IsAcquired);
        }

        using var rejectedLease = await limiter.AcquireAsync(firstCaller);
        using var otherCallerLease = await limiter.AcquireAsync(secondCaller);

        Assert.False(rejectedLease.IsAcquired);
        Assert.True(otherCallerLease.IsAcquired);
    }

    [Fact]
    public async Task it_should_use_the_anonymous_partition_for_unauthenticated_requests()
    {
        using var limiter = CallerRateLimiter.GetRateLimiter();
        var firstRequest = new DefaultHttpContext();
        var secondRequest = new DefaultHttpContext();

        for (var request = 0; request < 120; request++)
        {
            using var lease = await limiter.AcquireAsync(firstRequest);
            Assert.True(lease.IsAcquired);
        }

        using var rejectedLease = await limiter.AcquireAsync(secondRequest);

        Assert.False(rejectedLease.IsAcquired);
    }

    [Fact]
    public async Task it_should_tell_a_rejected_caller_when_to_retry()
    {
        using var limiter = CallerRateLimiter.GetRateLimiter();
        var caller = CreateContext("inspector");

        for (var request = 0; request < 120; request++)
        {
            using var lease = await limiter.AcquireAsync(caller);
        }

        using var rejectedLease = await limiter.AcquireAsync(caller);
        await CallerRateLimiter.AddRetryAfterHeader(
            new OnRejectedContext { HttpContext = caller, Lease = rejectedLease },
            CancellationToken.None);

        var seconds = int.Parse(caller.Response.Headers.RetryAfter!);
        Assert.InRange(seconds, 1, 60);
    }

    private static HttpContext CreateContext(string callerName)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, callerName)],
            authenticationType: "Bearer"));
        return context;
    }
}
