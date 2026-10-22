using Microsoft.Data.SqlClient;
using TradingDataGenerator;

namespace XTests.DataGenerator;

public class When_Connection_String_Is_Resolved : IDisposable
{
    private const string ArgumentConnectionString = "Server=argument;Database=TradingDemo";
    private const string EnvironmentConnectionString = "Server=environment;Database=TradingDemo";
    private static readonly DateTime Today = new(2026, 3, 15);

    private readonly TempDirectory _directory = new();

    public void Dispose() => _directory.Dispose();

    private string Resolve(string[] args, string? environmentConnectionString = null, string? currentDirectory = null) =>
        GeneratorOptions.Parse(args, environmentConnectionString, Today, currentDirectory ?? _directory.Path).ConnectionString;

    private string ResolveError(string? currentDirectory = null) =>
        Assert.ThrowsAny<InvalidOptionsException>(() => Resolve([], currentDirectory: currentDirectory)).Message;

    [Fact]
    public void it_should_prefer_the_argument_over_the_environment_variable_and_the_env_file()
    {
        _directory.WriteEnvFile("SA_PASSWORD=FromEnvFile-123");

        Assert.Equal(ArgumentConnectionString, Resolve([ArgumentConnectionString], EnvironmentConnectionString));
    }

    [Fact]
    public void it_should_prefer_the_environment_variable_over_the_env_file()
    {
        _directory.WriteEnvFile("SA_PASSWORD=FromEnvFile-123");

        Assert.Equal(EnvironmentConnectionString, Resolve([], EnvironmentConnectionString));
    }

    [Fact]
    public void it_should_build_the_docker_connection_string_from_the_env_file()
    {
        _directory.WriteEnvFile("SA_PASSWORD=FromEnvFile-123");

        var connection = new SqlConnectionStringBuilder(Resolve([]));

        Assert.Equal("localhost,1433", connection.DataSource);
        Assert.Equal("TradingDemo", connection.InitialCatalog);
        Assert.Equal("sa", connection.UserID);
        Assert.Equal("FromEnvFile-123", connection.Password);
        Assert.True(connection.TrustServerCertificate);
        Assert.Equal(5, connection.ConnectTimeout);
    }

    [Fact]
    public void it_should_find_the_env_file_in_a_parent_folder()
    {
        _directory.WriteEnvFile("SA_PASSWORD=FromEnvFile-123");
        var dataGeneratorFolder = _directory.CreateSubdirectory("DataGenerator");

        var connection = new SqlConnectionStringBuilder(Resolve([], currentDirectory: dataGeneratorFolder));

        Assert.Equal("FromEnvFile-123", connection.Password);
    }

    [Fact]
    public void it_should_use_db_name_and_ignore_comments_blank_lines_and_quotes()
    {
        _directory.WriteEnvFile("""
            # Local settings

            SA_PASSWORD="Quoted-Secret-123"
            MCP_READER_PASSWORD=Reader-123
            DB_NAME=OtherDemo
            """);

        var connection = new SqlConnectionStringBuilder(Resolve([]));

        Assert.Equal("Quoted-Secret-123", connection.Password);
        Assert.Equal("OtherDemo", connection.InitialCatalog);
    }

    [Fact]
    public void it_should_throw_a_custom_exception_when_there_is_no_env_file()
    {
        var exception = Assert.Throws<EnvFileNotFoundException>(() => Resolve([]));

        Assert.StartsWith($"No connection string, and no .env file found in {_directory.Path} or any folder above it.", exception.Message);
        Assert.Contains("copy .env.example to .env", exception.Message);
        Assert.Contains("TRADING_DB_CONNECTION", exception.Message);
    }

    [Fact]
    public void it_should_report_an_env_file_without_sa_password()
    {
        _directory.WriteEnvFile("MCP_READER_PASSWORD=Reader-123");

        Assert.EndsWith(".env doesn't set SA_PASSWORD. Copy the line from .env.example and set a password.", ResolveError());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void it_should_ignore_a_blank_argument_or_environment_variable(string blank)
    {
        _directory.WriteEnvFile("SA_PASSWORD=FromEnvFile-123");

        var connection = new SqlConnectionStringBuilder(Resolve([blank], blank));

        Assert.Equal("FromEnvFile-123", connection.Password);
    }
}
