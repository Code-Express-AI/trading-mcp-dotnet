using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation;

internal static class AccountGenerator
{
    // Every account is opened at least this many days before the data ends, so each one has some history.
    private const int MinimumDaysOfHistory = 120;

    public static List<Account> Generate(
        Random random,
        IReadOnlyList<Client> clients,
        IReadOnlyList<Instrument> instruments,
        DateTime firstTradingDay,
        DateTime lastTradingDay)
    {
        // Accounts only trade instruments in their own currency, so only offer currencies that have instruments.
        // Keeping the duplicates in SeedData.Currencies keeps the weighting, so most accounts are in USD.
        var currencies = SeedData.Currencies.Where(currency => instruments.Any(instrument => instrument.Currency == currency)).ToList();
        var latestOpeningDay = Math.Max(1, (lastTradingDay - firstTradingDay).Days - MinimumDaysOfHistory);
        var usedAccountNumbers = new HashSet<string>();
        var accounts = new List<Account>();

        foreach (var client in clients)
        {
            var accountCount = ChooseAccountCount(random);
            for (var i = 0; i < accountCount; i++)
            {
                accounts.Add(new Account(
                    Id: accounts.Count + 1,
                    ClientId: client.Id,
                    AccountNumber: CreateUniqueAccountNumber(random, usedAccountNumbers),
                    AccountType: random.Pick(SeedData.AccountTypes),
                    BaseCurrency: random.Pick(currencies),
                    OpenedOn: firstTradingDay.AddDays(random.Next(0, latestOpeningDay))));
            }
        }

        return accounts;
    }

    // 60% of clients have one account, 30% have two and 10% have three.
    private static int ChooseAccountCount(Random random) => random.NextDouble() switch
    {
        < 0.6 => 1,
        < 0.9 => 2,
        _ => 3
    };

    private static string CreateUniqueAccountNumber(Random random, HashSet<string> usedAccountNumbers)
    {
        string accountNumber;
        do
        {
            accountNumber = $"ACC{random.Next(10_000_000, 99_999_999)}";
        }
        while (!usedAccountNumbers.Add(accountNumber));

        return accountNumber;
    }
}
