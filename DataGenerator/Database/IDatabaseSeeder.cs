using TradingDataGenerator.Models;

namespace TradingDataGenerator.Database;

public interface IDatabaseSeeder
{
    Task<int> SeedAsync(GeneratedData generatedData);
    Task<int> SeedAsyncWithReset(GeneratedData generatedData);
}
