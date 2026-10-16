namespace TradingDataGenerator.Models;

public sealed class GeneratedData
{
    private List<Instrument>? _instruments;
    private List<PriceBar>? _prices;
    private List<Client>? _clients;
    private List<Account>? _accounts;
    private List<Trade>? _trades;

    private GeneratedData(
        List<Instrument> instruments,
        List<PriceBar> prices,
        List<Client> clients,
        List<Account> accounts,
        List<Trade> trades)
    {
        Instruments = instruments;
        Prices = prices;
        Clients = clients;
        Accounts = accounts;
        Trades = trades;
    }

    public GeneratedData()
    {
    }

    public List<Instrument> Instruments { get; private set; } = new();
    public List<PriceBar> Prices { get; private set; } = new();
    public List<Client> Clients { get; private set; } = new();
    public List<Account> Accounts { get; private set; } = new();
    public List<Trade> Trades { get; private set; } = new();

    public GeneratedData WithInstruments(List<Instrument> instruments)
    {
        _instruments = instruments ?? throw new ArgumentNullException(nameof(instruments));
        return this;
    }

    public GeneratedData WithPrices(List<PriceBar> prices)
    {
        _prices = prices ?? throw new ArgumentNullException(nameof(prices));
        return this;
    }

    public GeneratedData WithClients(List<Client> clients)
    {
        _clients = clients ?? throw new ArgumentNullException(nameof(clients));
        return this;
    }

    public GeneratedData WithAccounts(List<Account> accounts)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        return this;
    }

    public GeneratedData WithTrades(List<Trade> trades)
    {
        _trades = trades ?? throw new ArgumentNullException(nameof(trades));
        return this;
    }

    public GeneratedData Build()
    {
        List<Instrument> instruments;
        List<PriceBar> prices;
        List<Client> clients;
        List<Account> accounts;
        List<Trade> trades;
        GeneratedData generatedDataToReturn;

        instruments = _instruments ?? throw new InvalidOperationException("Instruments have not been provided.");
        prices = _prices ?? throw new InvalidOperationException("Prices have not been provided.");
        clients = _clients ?? throw new InvalidOperationException("Clients have not been provided.");
        accounts = _accounts ?? throw new InvalidOperationException("Accounts have not been provided.");
        trades = _trades ?? throw new InvalidOperationException("Trades have not been provided.");

        generatedDataToReturn = new GeneratedData(instruments, prices, clients, accounts, trades);
        return generatedDataToReturn;
    }
}
