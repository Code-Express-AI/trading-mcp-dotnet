using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation;

internal static class SyntheticData
{
    // One seeded Random drives everything, so the same seed and end date always produce the same data.
    public static GeneratedData Generate(GeneratorOptions options)
    {
        var endDate = options.EndDate;
        var startDate = endDate.AddYears(-options.Years);
        Console.WriteLine($"Generating synthetic data (seed {options.Seed}, {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})...");

        var random = new Random(options.Seed);
        var tradingDays = TradingCalendar.GetTradingDays(startDate, endDate);
        var instruments = InstrumentGenerator.Generate(random, options.InstrumentCount);
        var prices = PriceGenerator.Generate(random, instruments, tradingDays);
        var clients = ClientGenerator.Generate(options.Seed, options.ClientCount, tradingDays[0]);
        var accounts = AccountGenerator.Generate(random, clients, instruments, tradingDays[0], tradingDays[^1]);
        var trades = TradeGenerator.Generate(random, accounts, instruments, prices, tradingDays, options.TradesPerAccount);

        return new GeneratedData(instruments, prices, clients, accounts, trades);
    }
}
