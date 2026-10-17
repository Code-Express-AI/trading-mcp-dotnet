using TradingDataGenerator.Generation.Builders;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Instrument_Is_Built
{
    [Fact]
    public void it_should_create_an_instrument_with_the_supplied_values()
    {
        Instrument instrument;

        instrument = new InstrumentBuilder()
            .WithId(4)
            .WithTicker("EXMP")
            .WithName("Example Holdings")
            .WithSector("Financials")
            .WithCurrency("USD")
            .WithStartPrice(125.50)
            .WithMu(0.07)
            .WithSigma(0.22)
            .Build();

        Assert.Equal(4, instrument.Id);
        Assert.Equal("EXMP", instrument.Ticker);
        Assert.Equal("Example Holdings", instrument.Name);
        Assert.Equal("Financials", instrument.Sector);
        Assert.Equal("USD", instrument.Currency);
        Assert.Equal(125.50, instrument.StartPrice);
        Assert.Equal(0.07, instrument.Mu);
        Assert.Equal(0.22, instrument.Sigma);
    }

    [Theory]
    [InlineData("id", "Instrument ID has not been provided.")]
    [InlineData("ticker", "Instrument ticker has not been provided.")]
    [InlineData("name", "Instrument name has not been provided.")]
    [InlineData("sector", "Instrument sector has not been provided.")]
    [InlineData("currency", "Instrument currency has not been provided.")]
    [InlineData("start-price", "Instrument start price has not been provided.")]
    [InlineData("drift", "Instrument drift has not been provided.")]
    [InlineData("volatility", "Instrument volatility has not been provided.")]
    public void it_should_throw_when_a_required_value_is_missing(string missingValue, string expectedMessage)
    {
        InstrumentBuilder builder;
        InvalidOperationException exception;

        builder = new InstrumentBuilder();
        if (missingValue != "id")
        {
            builder.WithId(1);
        }

        if (missingValue != "ticker")
        {
            builder.WithTicker("EXMP");
        }

        if (missingValue != "name")
        {
            builder.WithName("Example Holdings");
        }

        if (missingValue != "sector")
        {
            builder.WithSector("Financials");
        }

        if (missingValue != "currency")
        {
            builder.WithCurrency("USD");
        }

        if (missingValue != "start-price")
        {
            builder.WithStartPrice(100);
        }

        if (missingValue != "drift")
        {
            builder.WithMu(0.05);
        }

        if (missingValue != "volatility")
        {
            builder.WithSigma(0.2);
        }

        exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void it_should_reject_null_string_values()
    {
        InstrumentBuilder builder;

        builder = new InstrumentBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.WithTicker(null!));
        Assert.Throws<ArgumentNullException>(() => builder.WithName(null!));
        Assert.Throws<ArgumentNullException>(() => builder.WithSector(null!));
        Assert.Throws<ArgumentNullException>(() => builder.WithCurrency(null!));
    }
}
