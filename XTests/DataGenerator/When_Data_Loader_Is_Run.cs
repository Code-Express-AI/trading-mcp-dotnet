using TradingDataGenerator;
using TradingDataGenerator.Database;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Data_Loader_Is_Run : IDisposable
{
    private const string ConnectionString = "Server=localhost,1433;Database=TradingDemo;User ID=sa;Password=Secret-123";
    private static readonly string[] SmallDataSet = ["--years=1", "--instruments=2", "--clients=2", "--trades-per-account=2"];

    private readonly TempDirectory _directory = new();
    private readonly StringWriter _output = new();
    private readonly StringWriter _errorOutput = new();

    public void Dispose() => _directory.Dispose();

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public async Task it_should_print_help_without_needing_a_connection_string(string helpOption)
    {
        var seederCreated = false;
        var loader = new DataLoader(_ => { seederCreated = true; return new FakeDatabaseSeeder(); }, _output, _errorOutput);

        var exitCode = await loader.RunAsync([helpOption, "--unknown-option"], environmentConnectionString: null, _directory.Path);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.Contains("Usage:", _output.ToString());
        Assert.Empty(_errorOutput.ToString());
        Assert.False(seederCreated);
    }

    [Fact]
    public async Task it_should_return_invalid_options_without_touching_the_database_when_an_option_is_invalid()
    {
        var seederCreated = false;
        var loader = new DataLoader(_ => { seederCreated = true; return new FakeDatabaseSeeder(); }, _output, _errorOutput);

        var exitCode = await loader.RunAsync(["--instruments=0"], ConnectionString, _directory.Path);

        Assert.Equal(ExitCodes.InvalidOptions, exitCode);
        Assert.Equal("--instruments must be between 1 and 375.", _errorOutput.ToString().Trim());
        Assert.False(seederCreated);
    }

    [Fact]
    public async Task it_should_seed_an_empty_database_without_clearing_it()
    {
        var seeder = new FakeDatabaseSeeder(containsData: false);
        var loader = new DataLoader(connectionString =>
        {
            Assert.Equal(ConnectionString, connectionString);
            return seeder;
        }, _output, _errorOutput);

        var exitCode = await loader.RunAsync(SmallDataSet, ConnectionString, _directory.Path);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.NotNull(seeder.SeededData);
        Assert.False(seeder.ClearedExistingData);
    }

    [Fact]
    public async Task it_should_refuse_to_seed_a_database_that_already_contains_data_without_reset()
    {
        var seeder = new FakeDatabaseSeeder(containsData: true);
        var loader = new DataLoader(_ => seeder, _output, _errorOutput);

        var exitCode = await loader.RunAsync(SmallDataSet, ConnectionString, _directory.Path);

        Assert.Equal(ExitCodes.DatabaseAlreadyContainsData, exitCode);
        Assert.Equal(
            "The database already contains data. Run again with --reset to delete it and load new data.",
            _errorOutput.ToString().Trim());
        Assert.Null(seeder.SeededData);
    }

    [Fact]
    public async Task it_should_clear_and_reseed_a_database_that_already_contains_data_with_reset()
    {
        var seeder = new FakeDatabaseSeeder(containsData: true);
        var loader = new DataLoader(_ => seeder, _output, _errorOutput);

        var exitCode = await loader.RunAsync([.. SmallDataSet, "--reset"], ConnectionString, _directory.Path);

        Assert.Equal(ExitCodes.Success, exitCode);
        Assert.NotNull(seeder.SeededData);
        Assert.True(seeder.ClearedExistingData);
    }

    // Makes a real connection attempt to a port nothing listens on.
    [Fact]
    [Trait("Category", "Integration")]
    public async Task it_should_explain_the_problem_when_sql_server_cannot_be_reached()
    {
        var loader = new DataLoader(connectionString => new DatabaseSeeder(connectionString), _output, _errorOutput);

        var exitCode = await loader.RunAsync([], "Server=127.0.0.1,1;Database=TradingDemo;User ID=sa;Password=x;Connect Timeout=5", _directory.Path);

        Assert.Equal(ExitCodes.DatabaseError, exitCode);
        Assert.StartsWith("Couldn't connect to SQL Server at 127.0.0.1,1.", _errorOutput.ToString());
    }

    private sealed class FakeDatabaseSeeder(bool containsData = false) : IDatabaseSeeder
    {
        public GeneratedData? SeededData { get; private set; }
        public bool ClearedExistingData { get; private set; }

        public Task<bool> ContainsDataAsync() => Task.FromResult(containsData);

        public Task SeedAsync(GeneratedData data, bool clearExistingData)
        {
            SeededData = data;
            ClearedExistingData = clearExistingData;
            return Task.CompletedTask;
        }
    }
}
