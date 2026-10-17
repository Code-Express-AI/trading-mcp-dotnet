using System.Security.Claims;
using TradingMcp.Auth;

namespace XTests.McpServer;

public class When_Caller_Identity_Is_Resolved
{
    [Fact]
    public void it_should_return_anonymous_for_a_missing_or_unauthenticated_user()
    {
        ClaimsPrincipal unauthenticatedUser;

        unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")]));

        Assert.Equal("anonymous", CallerIdentity.GetCallerId(null));
        Assert.Equal("anonymous", unauthenticatedUser.GetCallerId());
    }

    [Fact]
    public void it_should_use_the_authenticated_identity_name()
    {
        ClaimsPrincipal user;

        user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "inspector")],
            authenticationType: "Bearer"));

        Assert.Equal("inspector", user.GetCallerId());
    }

    [Fact]
    public void it_should_prefer_preferred_username_and_include_the_object_id()
    {
        ClaimsPrincipal user;

        user = CreateAuthenticatedUser(
            new Claim("preferred_username", "alice@example.com"),
            new Claim(ClaimTypes.Name, "fallback-name"),
            new Claim("oid", "object-123"));

        Assert.Equal("alice@example.com [oid:object-123]", user.GetCallerId());
    }

    [Theory]
    [InlineData("azp", "authorized-client")]
    [InlineData("appid", "legacy-client")]
    public void it_should_identify_an_app_by_its_client_claim(string claimType, string clientId)
    {
        ClaimsPrincipal user;

        user = CreateAuthenticatedUser(new Claim(claimType, clientId));

        Assert.Equal($"app:{clientId}", user.GetCallerId());
    }

    [Fact]
    public void it_should_fall_back_to_subject_then_unknown_for_an_app_without_a_name()
    {
        ClaimsPrincipal userWithSubject;
        ClaimsPrincipal userWithoutName;

        userWithSubject = CreateAuthenticatedUser(new Claim("sub", "subject-123"));
        userWithoutName = CreateAuthenticatedUser();

        Assert.Equal("subject-123", userWithSubject.GetCallerId());
        Assert.Equal("unknown", userWithoutName.GetCallerId());
    }

    private static ClaimsPrincipal CreateAuthenticatedUser(params Claim[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }
}
