using TradingDataGenerator.Models;
using TradingDataGenerator.Configuration;

namespace TradingDataGenerator.Generation;

public interface IDataPreparation
{
    GeneratedData PrepareGeneratedData(IGeneratorOptions generatorOptions);
}
