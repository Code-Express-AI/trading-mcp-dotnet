using TradingDataGenerator.Models;
using TradingDataGenerator.Generation.Builders;

namespace TradingDataGenerator.Generation.Generators;

public sealed class AccountsGenerator : IAccountsGenerator
{
    public List<Account> GenerateAccounts(
        Random randomNumberGenerator,
        List<Client> generatedClients,
        DateTime firstTradingDay,
        DateTime lastTradingDay,
        IReadOnlyList<string> availableCurrencies)
    {
        HashSet<string> usedAccountNumbers;
        int latestAccountOpenOffsetDays;
        List<Account> generatedAccountsToReturn;
        int accountCount;
        double accountSelectionValue;
        int accountIndex;
        string accountNumber;

        usedAccountNumbers = new HashSet<string>();
        latestAccountOpenOffsetDays = Math.Max(1, (int)(lastTradingDay - firstTradingDay).TotalDays - 120);
        generatedAccountsToReturn = new List<Account>();
        foreach (var generatedClient in generatedClients)
        {
            accountCount = 3;
            accountSelectionValue = randomNumberGenerator.NextDouble();
            if (accountSelectionValue < 0.6)
            {
                accountCount = 1;
            }
            else if (accountSelectionValue < 0.9)
            {
                accountCount = 2;
            }

            for (accountIndex = 0; accountIndex < accountCount; accountIndex++)
            {
                do
                {
                    accountNumber = $"ACC{randomNumberGenerator.Next(10_000_000, 99_999_999)}";
                }
                while (!usedAccountNumbers.Add(accountNumber));

                generatedAccountsToReturn.Add(new AccountBuilder()
                    .WithId(generatedAccountsToReturn.Count + 1)
                    .WithClientId(generatedClient.Id)
                    .WithAccountNumber(accountNumber)
                    .WithAccountType(SeedData.AccountTypes[randomNumberGenerator.Next(SeedData.AccountTypes.Count)])
                    .WithBaseCurrency(availableCurrencies[randomNumberGenerator.Next(availableCurrencies.Count)])
                    .WithOpenedOn(firstTradingDay.AddDays(randomNumberGenerator.Next(0, latestAccountOpenOffsetDays)))
                    .Build());
            }
        }
        return generatedAccountsToReturn;
    }
}
