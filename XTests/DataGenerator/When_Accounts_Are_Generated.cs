using TradingDataGenerator.Generation;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Accounts_Are_Generated
{
    private static readonly DateTime FirstTradingDay = new(2024, 1, 1);
    private static readonly DateTime LastTradingDay = new(2025, 1, 1);

    private static readonly List<Client> Clients = ClientGenerator.Generate(1234, 50, FirstTradingDay);

    [Fact]
    public void it_should_give_every_client_between_one_and_three_accounts_with_unique_numbers()
    {
        var accounts = AccountGenerator.Generate(new Random(1234), Clients, CreateInstruments("USD"), FirstTradingDay, LastTradingDay);

        Assert.Equal(accounts.Count, accounts.Select(account => account.AccountNumber).Distinct().Count());
        Assert.All(Clients, client => Assert.InRange(accounts.Count(account => account.ClientId == client.Id), 1, 3));
    }

    [Fact]
    public void it_should_open_accounts_early_enough_to_leave_some_trading_history()
    {
        var accounts = AccountGenerator.Generate(new Random(1234), Clients, CreateInstruments("USD"), FirstTradingDay, LastTradingDay);

        Assert.All(accounts, account => Assert.InRange(account.OpenedOn, FirstTradingDay, LastTradingDay.AddDays(-120)));
    }

    [Fact]
    public void it_should_only_use_currencies_that_have_instruments()
    {
        var accounts = AccountGenerator.Generate(new Random(1234), Clients, CreateInstruments("GBP", "EUR"), FirstTradingDay, LastTradingDay);

        Assert.All(accounts, account => Assert.Contains(account.BaseCurrency, new[] { "GBP", "EUR" }));
    }

    private static List<Instrument> CreateInstruments(params string[] currencies) =>
        currencies.Select((currency, index) => new Instrument(index + 1, $"TST{index}", "Test", "Financials", currency, 100, 0.07, 0.2)).ToList();
}
