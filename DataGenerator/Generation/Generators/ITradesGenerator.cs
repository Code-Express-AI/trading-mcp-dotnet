using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation.Generators;

public interface ITradesGenerator
{
    List<Trade> GenerateTrades(
        Random randomNumberGenerator,
        List<Account> generatedAccounts,
        List<Instrument> generatedInstruments,
        Dictionary<(int InstrumentId, DateTime TradingDate), decimal> closingPricesByInstrumentAndDate,
        List<DateTime> tradingDays,
        int averageTradesPerAccount);
}
