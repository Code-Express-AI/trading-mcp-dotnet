using System.Net.Sockets;
using System.Security.Authentication;
using TradingDataGenerator.Database;

namespace XTests.DataGenerator;

public class When_Database_Errors_Are_Described
{
    private const string ConnectionString = "Server=localhost,1433;Database=TradingDemo;User ID=sa;Password=Secret-123";

    private static string Describe(int errorNumber, params Exception[] causes) =>
        DatabaseErrorMessages.Describe(errorNumber, "Original SQL Server message.", causes, ConnectionString);

    [Fact]
    public void it_should_explain_how_to_start_the_container_when_the_connection_fails_on_macos_or_linux()
    {
        var message = Describe(0);

        Assert.StartsWith("Couldn't connect to SQL Server at localhost,1433.", message);
        Assert.Contains("docker compose up -d", message);
        Assert.Contains("port 1433 is already allocated", message);
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(53)]
    [InlineData(10060)]
    [InlineData(10061)]
    public void it_should_explain_how_to_start_the_container_when_the_connection_fails_on_windows(int errorNumber)
    {
        Assert.StartsWith("Couldn't connect to SQL Server at localhost,1433.", Describe(errorNumber));
    }

    [Fact]
    public void it_should_say_the_server_name_is_wrong_when_the_host_is_not_found_on_macos_or_linux()
    {
        Assert.StartsWith("Couldn't find a server called 'localhost,1433'.", Describe(0, new SocketException((int)SocketError.HostNotFound)));
    }

    [Fact]
    public void it_should_say_the_server_name_is_wrong_when_the_host_is_not_found_on_windows()
    {
        Assert.StartsWith("Couldn't find a server called 'localhost,1433'.", Describe(11001));
    }

    [Fact]
    public void it_should_suggest_trusting_the_certificate_when_it_is_not_trusted()
    {
        var message = Describe(0, new AuthenticationException());

        Assert.StartsWith("SQL Server's certificate isn't trusted.", message);
        Assert.Contains("TrustServerCertificate=True", message);
    }

    [Fact]
    public void it_should_point_at_the_password_and_a_port_conflict_when_the_login_fails()
    {
        var message = Describe(18456);

        Assert.StartsWith("SQL Server at localhost,1433 rejected the login for user 'sa'.", message);
        Assert.Contains("SA_PASSWORD", message);
        Assert.Contains("docker compose down -v", message);
        Assert.Contains("Stop that SQL Server service", message);
    }

    [Fact]
    public void it_should_suggest_running_db_init_when_the_database_does_not_exist()
    {
        var message = Describe(4060);

        Assert.StartsWith("The database 'TradingDemo' doesn't exist on localhost,1433.", message);
        Assert.Contains("docker compose run --rm db-init", message);
    }

    [Fact]
    public void it_should_suggest_running_db_init_when_the_tables_do_not_exist()
    {
        var message = Describe(208);

        Assert.StartsWith("The trading tables don't exist in 'TradingDemo'.", message);
        Assert.Contains("docker compose run --rm -e RECREATE=1 db-init", message);
    }

    [Theory]
    [InlineData(229)]
    [InlineData(4834)]
    public void it_should_suggest_an_admin_login_when_permission_is_denied(int errorNumber)
    {
        Assert.StartsWith("User 'sa' doesn't have permission to load data.", Describe(errorNumber));
    }

    [Fact]
    public void it_should_fall_back_to_the_original_message_for_other_errors()
    {
        Assert.Equal("SQL Server error 1205: Original SQL Server message.", Describe(1205));
    }
}
