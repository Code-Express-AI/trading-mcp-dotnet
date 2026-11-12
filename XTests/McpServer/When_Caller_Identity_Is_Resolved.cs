using System.Security.Claims;
using TradingMcp.Auth;

namespace XTests.McpServer;

public class When_Caller_Identity_Is_Resolved
{
    private static ClaimsPrincipal CreateAuthenticatedUser(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "Test"));

    [Fact]
    public void it_should_return_anonymous_for_a_missing_or_unauthenticated_user()
    {
        var unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")]));

        Assert.Equal("anonymous", CallerIdentity.GetCallerId(null));
        Assert.Equal("anonymous", unauthenticatedUser.GetCallerId());
    }

    [Fact]
    public void it_should_use_the_authenticated_identity_name()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "inspector")], authenticationType: "Bearer"));

        Assert.Equal("inspector", user.GetCallerId());
    }

    [Fact]
    public void it_should_prefer_preferred_username_and_include_the_object_id()
    {
        var user = CreateAuthenticatedUser(
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
        Assert.Equal($"app:{clientId}", CreateAuthenticatedUser(new Claim(claimType, clientId)).GetCallerId());
    }

    [Fact]
    public void it_should_key_a_static_token_on_its_name()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "copilot")], authenticationType: "Bearer"));

        Assert.Equal("copilot", user.GetCallerKey());
    }

    [Fact]
    public void it_should_key_an_entra_user_or_app_on_its_object_id_not_its_name()
    {
        var user = CreateAuthenticatedUser(new Claim("preferred_username", "alice@example.com"), new Claim("oid", "object-123"));
        var app = CreateAuthenticatedUser(new Claim("azp", "client-1"), new Claim("oid", "object-456"));

        Assert.Equal("object-123", user.GetCallerKey());
        Assert.Equal("object-456", app.GetCallerKey());
    }

    [Fact]
    public void it_should_have_no_key_for_a_missing_or_unauthenticated_user()
    {
        Assert.Null(CallerIdentity.GetCallerKey(null));
        Assert.Null(new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", "object-123")])).GetCallerKey());
    }

    [Fact]
    public void it_should_fall_back_to_subject_then_unknown_for_an_app_without_a_name()
    {
        Assert.Equal("subject-123", CreateAuthenticatedUser(new Claim("sub", "subject-123")).GetCallerId());
        Assert.Equal("unknown", CreateAuthenticatedUser().GetCallerId());
    }
}
