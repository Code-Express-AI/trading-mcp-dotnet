using TradingDataGenerator.Models;
using TradingDataGenerator.Generation.Generators;

namespace TradingDataGenerator.Generation;

public sealed class SyntheticDataGenerator : ISyntheticDataGenerator
{
    private readonly ITradingCalendarGenerator _tradingCalendarGenerator;
    private readonly IInstrumentGenerator _instrumentGenerator;
    private readonly IPriceBarGenerator _priceBarGenerator;
    private readonly IClientGenerator _clientGenerator;
    private readonly IAccountsGenerator _accountsGenerator;
    private readonly ITradesGenerator _tradesGenerator;

    public SyntheticDataGenerator()
        : this(
            new TradingCalendarGenerator(),
            new InstrumentGenerator(),
            new PriceBarGenerator(),
            new ClientGenerator(),
            new AccountsGenerator(),
            new TradesGenerator())
    {
    }

    public SyntheticDataGenerator(
        ITradingCalendarGenerator tradingCalendarGenerator,
        IInstrumentGenerator instrumentGenerator,
        IPriceBarGenerator priceBarGenerator,
        IClientGenerator clientGenerator,
        IAccountsGenerator accountsGenerator,
        ITradesGenerator tradesGenerator)
    {
        _tradingCalendarGenerator = tradingCalendarGenerator;
        _instrumentGenerator = instrumentGenerator;
        _priceBarGenerator = priceBarGenerator;
        _clientGenerator = clientGenerator;
        _accountsGenerator = accountsGenerator;
        _tradesGenerator = tradesGenerator;
    }

    public List<DateTime> GetTradingDays(DateTime startDate, DateTime endDate)
    {
        return _tradingCalendarGenerator.GetTradingDays(startDate, endDate);
    }

    public List<Instrument> GenerateInstruments(Random randomNumberGenerator, int instrumentCount)
    {
        return _instrumentGenerator.GenerateInstruments(randomNumberGenerator, instrumentCount);
    }

    public List<PriceBar> GeneratePriceBars(
        Random randomNumberGenerator,
        List<Instrument> generatedInstruments,
        List<DateTime> tradingDays)
    {
        return _priceBarGenerator.GeneratePriceBars(
            randomNumberGenerator, generatedInstruments, tradingDays);
    }

    public List<Client> GenerateClients(int randomSeed, int clientCount, DateTime firstTradingDay)
    {
        return _clientGenerator.GenerateClients(randomSeed, clientCount, firstTradingDay);
    }

    public List<Account> GenerateAccounts(
        Random randomNumberGenerator,
        List<Client> generatedClients,
        DateTime firstTradingDay,
        DateTime lastTradingDay,
        IReadOnlyList<string> availableCurrencies)
    {
        return _accountsGenerator.GenerateAccounts(
            randomNumberGenerator, generatedClients, firstTradingDay, lastTradingDay, availableCurrencies);
    }

    public List<Trade> GenerateTrades(
        Random randomNumberGenerator,
        List<Account> generatedAccounts,
        List<Instrument> generatedInstruments,
        Dictionary<(int InstrumentId, DateTime TradingDate), decimal> closingPricesByInstrumentAndDate,
        List<DateTime> tradingDays,
        int averageTradesPerAccount)
    {
        return _tradesGenerator.GenerateTrades(
            randomNumberGenerator,
            generatedAccounts,
            generatedInstruments,
            closingPricesByInstrumentAndDate,
            tradingDays,
            averageTradesPerAccount);
    }
}
