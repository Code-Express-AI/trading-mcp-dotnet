namespace XTests.Integration;

// Locally, the integration tests are skipped when SQL Server isn't running. On CI that would hide a broken
// database step behind a green build, so this one test fails there instead, with the reason.
[Trait("Category", "Integration")]
public class When_Ci_Build_Is_Run
{
    [Fact]
    public void it_should_have_a_database_for_the_integration_tests()
    {
        Assert.SkipWhen(Environment.GetEnvironmentVariable("CI") is null, "Only checked on CI.");

        Assert.True(IntegrationDatabase.SkipReason is null, IntegrationDatabase.SkipReason);
    }
}
