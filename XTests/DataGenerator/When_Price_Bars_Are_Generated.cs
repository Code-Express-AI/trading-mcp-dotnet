using TradingDataGenerator.Generation.Generators;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Price_Bars_Are_Generated
{
    [Fact]
    public void it_should_generate_valid_ohlc_values_for_each_instrument_and_day()
    {
        InstrumentGenerator instrumentGenerator;
        PriceBarGenerator priceBarGenerator;
        List<Instrument> instruments;
        List<DateTime> tradingDays;
        List<PriceBar> generatedPriceBars;

        instrumentGenerator = new InstrumentGenerator();
        priceBarGenerator = new PriceBarGenerator();
        instruments = instrumentGenerator.GenerateInstruments(new Random(123), 2);
        tradingDays = [new DateTime(2025, 1, 6), new DateTime(2025, 1, 7), new DateTime(2025, 1, 8)];
        generatedPriceBars = priceBarGenerator.GeneratePriceBars(new Random(456), instruments, tradingDays);

        Assert.Equal(instruments.Count * tradingDays.Count, generatedPriceBars.Count);
        Assert.All(generatedPriceBars, priceBar =>
        {
            Assert.True(priceBar.High >= priceBar.Open);
            Assert.True(priceBar.High >= priceBar.Close);
            Assert.True(priceBar.Low <= priceBar.Open);
            Assert.True(priceBar.Low <= priceBar.Close);
            Assert.True(priceBar.Volume > 0);
        });
    }

    [Fact]
    public void it_should_generate_the_same_price_bars_for_the_same_random_seed()
    {
        PriceBarGenerator priceBarGenerator;
        Instrument instrument;
        List<DateTime> tradingDays;
        List<PriceBar> firstGeneratedPriceBars;
        List<PriceBar> secondGeneratedPriceBars;

        priceBarGenerator = new PriceBarGenerator();
        instrument = new Instrument(1, "EXMP", "Example Holdings", "Financials", "USD", 100, 0.07, 0.22);
        tradingDays = [new DateTime(2025, 1, 6), new DateTime(2025, 1, 7)];
        firstGeneratedPriceBars = priceBarGenerator.GeneratePriceBars(new Random(456), [instrument], tradingDays);
        secondGeneratedPriceBars = priceBarGenerator.GeneratePriceBars(new Random(456), [instrument], tradingDays);

        Assert.Equal(firstGeneratedPriceBars, secondGeneratedPriceBars);
    }
}
