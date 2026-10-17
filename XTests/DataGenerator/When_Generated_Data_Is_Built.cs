using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Generated_Data_Is_Built
{
    [Fact]
    public void it_should_include_each_supplied_collection()
    {
        List<Instrument> instruments;
        List<PriceBar> priceBars;
        List<Client> clients;
        List<Account> accounts;
        List<Trade> trades;
        GeneratedData generatedData;

        instruments = [];
        priceBars = [];
        clients = [];
        accounts = [];
        trades = [];
        generatedData = new GeneratedData()
            .WithInstruments(instruments)
            .WithPrices(priceBars)
            .WithClients(clients)
            .WithAccounts(accounts)
            .WithTrades(trades)
            .Build();

        Assert.Same(instruments, generatedData.Instruments);
        Assert.Same(priceBars, generatedData.Prices);
        Assert.Same(clients, generatedData.Clients);
        Assert.Same(accounts, generatedData.Accounts);
        Assert.Same(trades, generatedData.Trades);
    }

    [Fact]
    public void it_should_throw_when_a_collection_is_missing()
    {
        InvalidOperationException exception;

        exception = Assert.Throws<InvalidOperationException>(() => new GeneratedData().Build());

        Assert.Equal("Instruments have not been provided.", exception.Message);
    }

    [Theory]
    [InlineData("instruments", "Instruments have not been provided.")]
    [InlineData("prices", "Prices have not been provided.")]
    [InlineData("clients", "Clients have not been provided.")]
    [InlineData("accounts", "Accounts have not been provided.")]
    [InlineData("trades", "Trades have not been provided.")]
    public void it_should_throw_when_each_required_collection_is_missing(string missingCollection, string expectedMessage)
    {
        GeneratedData builder;
        InvalidOperationException exception;

        builder = new GeneratedData();
        if (missingCollection != "instruments")
        {
            builder.WithInstruments([]);
        }

        if (missingCollection != "prices")
        {
            builder.WithPrices([]);
        }

        if (missingCollection != "clients")
        {
            builder.WithClients([]);
        }

        if (missingCollection != "accounts")
        {
            builder.WithAccounts([]);
        }

        if (missingCollection != "trades")
        {
            builder.WithTrades([]);
        }

        exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void it_should_reject_null_collections()
    {
        GeneratedData builder;

        builder = new GeneratedData();

        Assert.Throws<ArgumentNullException>(() => builder.WithInstruments(null!));
        Assert.Throws<ArgumentNullException>(() => builder.WithPrices(null!));
        Assert.Throws<ArgumentNullException>(() => builder.WithClients(null!));
        Assert.Throws<ArgumentNullException>(() => builder.WithAccounts(null!));
        Assert.Throws<ArgumentNullException>(() => builder.WithTrades(null!));
    }
}
