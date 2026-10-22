using TradingDataGenerator.Generation;

namespace XTests.DataGenerator;

public class When_Instruments_Are_Generated
{
    [Fact]
    public void it_should_generate_unique_names_and_tickers_with_sensible_start_prices()
    {
        var instruments = InstrumentGenerator.Generate(new Random(1234), 20);

        Assert.Equal(20, instruments.Count);
        Assert.Equal(Enumerable.Range(1, 20), instruments.Select(instrument => instrument.Id));
        Assert.Equal(20, instruments.Select(instrument => instrument.Name).Distinct().Count());
        Assert.Equal(20, instruments.Select(instrument => instrument.Ticker).Distinct().Count());
        Assert.All(instruments, instrument => Assert.InRange(instrument.StartPrice, 5, 600));
    }

    [Fact]
    public void it_should_be_able_to_generate_every_possible_company()
    {
        var instruments = InstrumentGenerator.Generate(new Random(1234), SeedData.MaxInstruments);

        Assert.Equal(SeedData.MaxInstruments, instruments.Select(instrument => instrument.Ticker).Distinct().Count());
    }

    [Fact]
    public void it_should_refuse_to_generate_more_companies_than_the_names_allow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => InstrumentGenerator.Generate(new Random(1234), SeedData.MaxInstruments + 1));
    }
}
