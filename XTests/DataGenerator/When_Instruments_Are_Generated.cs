using TradingDataGenerator.Generation.Generators;

namespace XTests.DataGenerator;

public class When_Instruments_Are_Generated
{
    [Fact]
    public void it_should_generate_unique_tickers_and_valid_start_prices()
    {
        InstrumentGenerator instrumentGenerator;
        List<TradingDataGenerator.Models.Instrument> generatedInstruments;

        instrumentGenerator = new InstrumentGenerator();
        generatedInstruments = instrumentGenerator.GenerateInstruments(new Random(1234), 20);

        Assert.Equal(20, generatedInstruments.Count);
        Assert.Equal(
            generatedInstruments.Count,
            generatedInstruments.Select(instrument => instrument.Ticker).Distinct().Count());
        Assert.All(generatedInstruments, instrument =>
        {
            Assert.InRange(instrument.StartPrice, 5, 600);
        });
    }
}
