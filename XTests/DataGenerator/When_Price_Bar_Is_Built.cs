using TradingDataGenerator.Generation.Builders;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Price_Bar_Is_Built
{
    [Fact]
    public void it_should_create_a_price_bar_with_the_supplied_values()
    {
        DateTime priceDate;
        PriceBar priceBar;

        priceDate = new DateTime(2025, 1, 15);
        priceBar = new PriceBarBuilder()
            .WithInstrumentId(4)
            .WithPriceDate(priceDate)
            .WithOpen(100.10m)
            .WithHigh(105.20m)
            .WithLow(99.50m)
            .WithClose(103.75m)
            .WithVolume(250_000)
            .Build();

        Assert.Equal(4, priceBar.InstrumentId);
        Assert.Equal(priceDate, priceBar.PriceDate);
        Assert.Equal(100.10m, priceBar.Open);
        Assert.Equal(105.20m, priceBar.High);
        Assert.Equal(99.50m, priceBar.Low);
        Assert.Equal(103.75m, priceBar.Close);
        Assert.Equal(250_000, priceBar.Volume);
    }

    [Theory]
    [InlineData("instrument-id", "Price bar instrument ID has not been provided.")]
    [InlineData("date", "Price bar date has not been provided.")]
    [InlineData("open", "Price bar open price has not been provided.")]
    [InlineData("high", "Price bar high price has not been provided.")]
    [InlineData("low", "Price bar low price has not been provided.")]
    [InlineData("close", "Price bar close price has not been provided.")]
    [InlineData("volume", "Price bar volume has not been provided.")]
    public void it_should_throw_when_a_required_value_is_missing(string missingValue, string expectedMessage)
    {
        PriceBarBuilder builder;
        InvalidOperationException exception;

        builder = new PriceBarBuilder();
        if (missingValue != "instrument-id")
        {
            builder.WithInstrumentId(1);
        }

        if (missingValue != "date")
        {
            builder.WithPriceDate(DateTime.UnixEpoch);
        }

        if (missingValue != "open")
        {
            builder.WithOpen(100);
        }

        if (missingValue != "high")
        {
            builder.WithHigh(101);
        }

        if (missingValue != "low")
        {
            builder.WithLow(99);
        }

        if (missingValue != "close")
        {
            builder.WithClose(100);
        }

        if (missingValue != "volume")
        {
            builder.WithVolume(1000);
        }

        exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Equal(expectedMessage, exception.Message);
    }
}
