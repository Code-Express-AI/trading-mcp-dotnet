namespace TradingDataGenerator.Models;

internal sealed record GeneratedData(
    IReadOnlyList<Instrument> Instruments,
    IReadOnlyList<InstrumentPrice> Prices,
    IReadOnlyList<Client> Clients,
    IReadOnlyList<Account> Accounts,
    IReadOnlyList<Trade> Trades);
