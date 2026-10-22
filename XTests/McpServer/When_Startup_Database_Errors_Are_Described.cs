using TradingMcp;

namespace XTests.McpServer;

public class When_Startup_Database_Errors_Are_Described
{
    private const string ConnectionString = "Server=localhost,1433;Database=TradingDemo;User ID=mcp_reader;Password=x";

    private static string Describe(int errorNumber) =>
        StartupDatabaseCheck.Describe(errorNumber, "Original SQL Server message.", ConnectionString);

    [Fact]
    public void it_should_point_at_the_reader_password_when_the_login_fails()
    {
        Assert.Equal(
            "SQL Server rejected the login for 'mcp_reader'. Check the password matches MCP_READER_PASSWORD in your .env file.",
            Describe(18456));
    }

    [Theory]
    [InlineData(4060)]
    [InlineData(2812)]
    [InlineData(229)]
    public void it_should_suggest_db_init_when_the_database_or_procedures_are_missing(int errorNumber)
    {
        Assert.EndsWith("Run: docker compose run --rm db-init", Describe(errorNumber));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10061)]
    public void it_should_suggest_checking_the_container_when_it_cannot_connect(int errorNumber)
    {
        Assert.Equal(
            "Couldn't connect to SQL Server at localhost,1433. Check the container is running: docker compose ps",
            Describe(errorNumber));
    }

    [Fact]
    public void it_should_fall_back_to_the_original_message()
    {
        Assert.Equal("Original SQL Server message.", Describe(1205));
    }
}
