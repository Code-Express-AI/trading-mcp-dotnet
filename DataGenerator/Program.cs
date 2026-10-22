using TradingDataGenerator.Database;

namespace TradingDataGenerator;

internal static class Program
{
    private static Task<int> Main(string[] args) =>
        new DataLoader(connectionString => new DatabaseSeeder(connectionString), Console.Out, Console.Error)
            .RunAsync(args, Environment.GetEnvironmentVariable("TRADING_DB_CONNECTION"), Directory.GetCurrentDirectory());
}
