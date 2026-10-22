using TradingDataGenerator.Generation;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Prices_Are_Generated
{
    private static readonly Instrument Instrument = new(1, "EXMH", "Example Holdings", "Financials", "USD", 100, 0.07, 0.22);

    [Fact]
    public void it_should_generate_one_valid_price_bar_per_instrument_per_day()
    {
        var instruments = InstrumentGenerator.Generate(new Random(123), 3);
        var tradingDays = TradingCalendar.GetTradingDays(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31));

        var prices = PriceGenerator.Generate(new Random(456), instruments, tradingDays);

        Assert.Equal(instruments.Count * tradingDays.Count, prices.Count);
        Assert.All(prices, price =>
        {
            Assert.True(price.Low > 0);
            Assert.True(price.Low <= price.Open && price.Low <= price.Close);
            Assert.True(price.High >= price.Open && price.High >= price.Close);
            Assert.True(price.Volume > 0);
        });
    }

    [Fact]
    public void it_should_generate_the_same_prices_for_the_same_seed()
    {
        var tradingDays = TradingCalendar.GetTradingDays(new DateTime(2025, 1, 6), new DateTime(2025, 1, 31));

        var first = PriceGenerator.Generate(new Random(456), [Instrument], tradingDays);
        var second = PriceGenerator.Generate(new Random(456), [Instrument], tradingDays);

        Assert.Equal(first, second);
    }
}
