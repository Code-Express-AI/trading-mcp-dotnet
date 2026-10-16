using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation.Generators;

public interface IInstrumentGenerator
{
    List<Instrument> GenerateInstruments(Random randomNumberGenerator, int instrumentCount);
}
