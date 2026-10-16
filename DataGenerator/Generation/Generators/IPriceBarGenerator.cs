using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation.Generators;

public interface IPriceBarGenerator
{
    List<PriceBar> GeneratePriceBars(
        Random randomNumberGenerator,
        List<Instrument> generatedInstruments,
        List<DateTime> tradingDays);
}
