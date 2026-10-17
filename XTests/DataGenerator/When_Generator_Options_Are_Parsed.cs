using TradingDataGenerator.Configuration;

namespace XTests.DataGenerator;

public class When_Generator_Options_Are_Parsed
{
    [Fact]
    public void it_should_use_default_values_when_no_options_are_provided()
    {
        GeneratorOptionsParseResult result;
        GeneratorOptions generatorOptions;

        result = GeneratorOptions.Parse(Array.Empty<string>());
        generatorOptions = result.Options ?? throw new InvalidOperationException(result.ErrorMessage);

        Assert.False(generatorOptions.Reset);
        Assert.Equal(42, generatorOptions.Seed);
        Assert.Equal(50, generatorOptions.InstrumentCount);
        Assert.Equal(200, generatorOptions.ClientCount);
        Assert.Equal(3, generatorOptions.Years);
        Assert.Equal(40, generatorOptions.TradesPerAccount);
    }

    [Fact]
    public void it_should_parse_explicit_values_and_reset_flag()
    {
        GeneratorOptionsParseResult result;
        GeneratorOptions generatorOptions;

        result = GeneratorOptions.Parse(
            ["--reset", "--seed=7", "--instruments=12", "--clients=30", "--years=2", "--trades-per-account=20"]);
        generatorOptions = result.Options ?? throw new InvalidOperationException(result.ErrorMessage);

        Assert.True(generatorOptions.Reset);
        Assert.Equal(7, generatorOptions.Seed);
        Assert.Equal(12, generatorOptions.InstrumentCount);
        Assert.Equal(30, generatorOptions.ClientCount);
        Assert.Equal(2, generatorOptions.Years);
        Assert.Equal(20, generatorOptions.TradesPerAccount);
    }

    [Theory]
    [InlineData("--seed=invalid", "Invalid --seed value 'invalid'. Expected an integer.")]
    [InlineData("--instruments=invalid", "Invalid --instruments value 'invalid'. Expected an integer.")]
    [InlineData("--clients=invalid", "Invalid --clients value 'invalid'. Expected an integer.")]
    [InlineData("--years=invalid", "Invalid --years value 'invalid'. Expected an integer.")]
    [InlineData("--trades-per-account=invalid", "Invalid --trades-per-account value 'invalid'. Expected an integer.")]
    [InlineData("--instruments=0", "--instruments must be greater than zero.")]
    [InlineData("--clients=-1", "--clients cannot be negative.")]
    [InlineData("--years=0", "--years must be greater than zero.")]
    [InlineData("--trades-per-account=1", "--trades-per-account must be between 2 and 1073741823.")]
    [InlineData("--trades-per-account=1073741824", "--trades-per-account must be between 2 and 1073741823.")]
    public void it_should_return_an_error_when_an_option_is_invalid(
        string invalidOption,
        string expectedErrorMessage)
    {
        GeneratorOptionsParseResult result;

        result = GeneratorOptions.Parse([invalidOption]);

        Assert.Null(result.Options);
        Assert.Equal(expectedErrorMessage, result.ErrorMessage);
    }

    [Fact]
    public void it_should_reject_a_year_count_beyond_the_available_date_range()
    {
        GeneratorOptionsParseResult result;

        result = GeneratorOptions.Parse(["--years=5000"]);

        Assert.Null(result.Options);
        Assert.Equal(
            $"--years cannot exceed {DateTime.UtcNow.Date.AddDays(-1).Year - 1} for the current date range.",
            result.ErrorMessage);
    }

    [Fact]
    public void it_should_ignore_non_options_and_use_the_last_value_for_duplicate_options()
    {
        GeneratorOptionsParseResult result;
        GeneratorOptions generatorOptions;

        result = GeneratorOptions.Parse(["connection-string", "--seed=7", "--seed=8"]);
        generatorOptions = result.Options ?? throw new InvalidOperationException(result.ErrorMessage);

        Assert.Equal(8, generatorOptions.Seed);
    }
}
