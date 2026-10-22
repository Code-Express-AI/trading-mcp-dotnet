using TradingDataGenerator.Models;

namespace TradingDataGenerator.Database;

internal interface IDatabaseSeeder
{
    Task<bool> ContainsDataAsync();
    Task SeedAsync(GeneratedData data, bool clearExistingData);
}
