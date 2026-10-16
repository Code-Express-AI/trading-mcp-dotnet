using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation;

public interface ISyntheticDataGenerator
{
    List<DateTime> GetTradingDays(DateTime startDate, DateTime endDate);

    List<Instrument> GenerateInstruments(Random randomNumberGenerator, int instrumentCount);

    List<PriceBar> GeneratePriceBars(
        Random randomNumberGenerator,
        List<Instrument> generatedInstruments,
        List<DateTime> tradingDays);

    List<Client> GenerateClients(int randomSeed, int clientCount, DateTime firstTradingDay);

    List<Account> GenerateAccounts(
        Random randomNumberGenerator,
        List<Client> generatedClients,
        DateTime firstTradingDay,
        DateTime lastTradingDay,
        IReadOnlyList<string> availableCurrencies);

    List<Trade> GenerateTrades(
        Random randomNumberGenerator,
        List<Account> generatedAccounts,
        List<Instrument> generatedInstruments,
        Dictionary<(int InstrumentId, DateTime TradingDate), decimal> closingPricesByInstrumentAndDate,
        List<DateTime> tradingDays,
        int averageTradesPerAccount);
}
