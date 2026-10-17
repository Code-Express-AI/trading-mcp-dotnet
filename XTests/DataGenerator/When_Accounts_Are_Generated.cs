using TradingDataGenerator.Generation.Generators;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Accounts_Are_Generated
{
    [Fact]
    public void it_should_create_unique_account_numbers_for_the_supplied_clients()
    {
        AccountsGenerator accountsGenerator;
        List<Client> clients;
        DateTime firstTradingDay;
        DateTime lastTradingDay;
        List<Account> generatedAccounts;

        accountsGenerator = new AccountsGenerator();
        firstTradingDay = new DateTime(2024, 1, 1);
        lastTradingDay = new DateTime(2025, 1, 1);
        clients =
        [
            new Client(1, "Avery Example", "avery@example.com", "United Kingdom", "Balanced", firstTradingDay),
            new Client(2, "Morgan Example", "morgan@example.com", "United States", "Growth", firstTradingDay)
        ];
        generatedAccounts = accountsGenerator.GenerateAccounts(
            new Random(1234), clients, firstTradingDay, lastTradingDay, ["USD", "GBP"]);

        Assert.NotEmpty(generatedAccounts);
        Assert.Equal(
            generatedAccounts.Count,
            generatedAccounts.Select(account => account.AccountNumber).Distinct().Count());
        Assert.All(generatedAccounts, account =>
        {
            Assert.Contains(account.ClientId, clients.Select(client => client.Id));
            Assert.Contains(account.BaseCurrency, new[] { "USD", "GBP" });
            Assert.InRange(account.OpenedOn, firstTradingDay, lastTradingDay);
        });
    }
}
