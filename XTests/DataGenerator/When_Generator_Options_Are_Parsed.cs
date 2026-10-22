using TradingDataGenerator;

namespace XTests.DataGenerator;

public class When_Generator_Options_Are_Parsed : IDisposable
{
    private const string ConnectionString = "Server=localhost,1433;Database=TradingDemo;User ID=sa;Password=Secret-123";
    private static readonly DateTime Today = new(2026, 3, 15);

    private readonly TempDirectory _directory = new();

    public void Dispose() => _directory.Dispose();

    private GeneratorOptions Parse(params string[] args) => GeneratorOptions.Parse(args, ConnectionString, Today, _directory.Path);

    private string ParseError(string[] args) =>
        Assert.Throws<InvalidOptionsException>(() => GeneratorOptions.Parse(args, ConnectionString, Today, _directory.Path)).Message;

    [Fact]
    public void it_should_use_default_values_when_no_options_are_provided()
    {
        var options = Parse();

        Assert.Equal(ConnectionString, options.ConnectionString);
        Assert.False(options.Reset);
        Assert.Equal(42, options.Seed);
        Assert.Equal(50, options.InstrumentCount);
        Assert.Equal(200, options.ClientCount);
        Assert.Equal(3, options.Years);
        Assert.Equal(40, options.TradesPerAccount);
        Assert.Equal(new DateTime(2026, 3, 14), options.EndDate);
    }

    [Fact]
    public void it_should_parse_explicit_values_and_the_reset_flag()
    {
        var options = Parse(
            "--reset", "--seed=7", "--instruments=12", "--clients=30", "--years=2", "--trades-per-account=20", "--end-date=2025-12-31");

        Assert.True(options.Reset);
        Assert.Equal(7, options.Seed);
        Assert.Equal(12, options.InstrumentCount);
        Assert.Equal(30, options.ClientCount);
        Assert.Equal(2, options.Years);
        Assert.Equal(20, options.TradesPerAccount);
        Assert.Equal(new DateTime(2025, 12, 31), options.EndDate);
    }

    [Fact]
    public void it_should_use_the_last_value_when_an_option_is_repeated()
    {
        Assert.Equal(8, Parse("--seed=7", "--seed=8").Seed);
    }

    [Fact]
    public void it_should_reject_a_connection_string_that_still_contains_a_placeholder()
    {
        var error = ParseError(["Server=localhost,1433;User ID=sa;Password=<SA_PASSWORD>"]);

        Assert.StartsWith("The connection string still contains a placeholder such as <SA_PASSWORD>.", error);
    }

    [Fact]
    public void it_should_reject_a_badly_formatted_connection_string()
    {
        Assert.StartsWith("The connection string isn't valid:", ParseError(["not a connection string"]));
    }

    [Fact]
    public void it_should_reject_an_unknown_option()
    {
        Assert.Equal(
            "Unknown option '--instrument'. Run with --help to see the valid options.",
            ParseError(["--instrument=50"]));
    }

    [Fact]
    public void it_should_reject_a_value_written_with_a_space_instead_of_an_equals_sign()
    {
        Assert.Equal(
            "Unexpected argument '42'. Write options as --name=value, for example --seed=42.",
            ParseError([ConnectionString, "--seed", "42"]));
    }

    [Fact]
    public void it_should_reject_a_value_for_the_reset_flag()
    {
        Assert.Equal("--reset doesn't take a value. Use --reset on its own.", ParseError(["--reset=false"]));
    }

    [Theory]
    [InlineData("--seed=abc", "Invalid --seed value 'abc'. Expected a whole number, for example --seed=42.")]
    [InlineData("--instruments=1.5", "Invalid --instruments value '1.5'. Expected a whole number, for example --instruments=50.")]
    [InlineData("--seed=-1", "--seed must be between 0 and 2147483647.")]
    [InlineData("--instruments=0", "--instruments must be between 1 and 375.")]
    [InlineData("--instruments=376", "--instruments must be between 1 and 375.")]
    [InlineData("--clients=0", "--clients must be between 1 and 10000.")]
    [InlineData("--years=11", "--years must be between 1 and 10.")]
    [InlineData("--trades-per-account=1", "--trades-per-account must be between 2 and 1000.")]
    [InlineData("--end-date=15/03/2026", "Invalid --end-date value '15/03/2026'. Expected yyyy-MM-dd, for example --end-date=2026-01-30.")]
    [InlineData("--end-date=2026-03-15", "--end-date must be between 2000-01-01 and yesterday (2026-03-14).")]
    [InlineData("--end-date=1999-12-31", "--end-date must be between 2000-01-01 and yesterday (2026-03-14).")]
    public void it_should_reject_an_invalid_value(string option, string expectedError)
    {
        Assert.Equal(expectedError, ParseError([option]));
    }

    [Fact]
    public void it_should_describe_every_option_in_the_help()
    {
        Assert.All(GeneratorOptions.KnownOptions, option => Assert.Contains($"--{option}", GeneratorOptions.HelpText));
    }
}
