using TradingDataGenerator;
using TradingDataGenerator.Generation;

namespace XTests.DataGenerator;

public class When_Synthetic_Data_Is_Generated
{
    private static readonly GeneratorOptions Options = GeneratorOptions.Parse(
        ["--years=1", "--instruments=5", "--clients=10", "--trades-per-account=5", "--end-date=2025-12-31"],
        "Server=localhost;Database=TradingDemo",
        new DateTime(2026, 3, 15),
        Path.GetTempPath());

    [Fact]
    public void it_should_generate_data_whose_references_all_line_up()
    {
        var data = SyntheticData.Generate(Options);

        Assert.Equal(5, data.Instruments.Count);
        Assert.Equal(10, data.Clients.Count);
        Assert.NotEmpty(data.InstrumentPrices);
        Assert.NotEmpty(data.Accounts);
        Assert.NotEmpty(data.Trades);
        Assert.All(data.Accounts, account => Assert.Contains(data.Clients, client => client.Id == account.ClientId));
        Assert.All(data.Trades, trade =>
        {
            Assert.Contains(data.Accounts, account => account.Id == trade.AccountId);
            Assert.Contains(data.Instruments, instrument => instrument.Id == trade.InstrumentId);
        });
    }

    [Fact]
    public void it_should_end_on_the_requested_date()
    {
        var data = SyntheticData.Generate(Options);

        Assert.Equal(new DateTime(2025, 12, 31), data.InstrumentPrices.Max(price => price.PriceDate));
    }

    [Fact]
    public void it_should_generate_identical_data_for_the_same_seed_and_end_date()
    {
        var first = SyntheticData.Generate(Options);
        var second = SyntheticData.Generate(Options);

        Assert.Equal(first.Instruments, second.Instruments);
        Assert.Equal(first.InstrumentPrices, second.InstrumentPrices);
        Assert.Equal(first.Clients, second.Clients);
        Assert.Equal(first.Accounts, second.Accounts);
        Assert.Equal(first.Trades, second.Trades);
    }
}
