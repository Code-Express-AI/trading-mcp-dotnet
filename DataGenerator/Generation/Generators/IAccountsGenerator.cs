using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation.Generators;

public interface IAccountsGenerator
{
    List<Account> GenerateAccounts(
        Random randomNumberGenerator,
        List<Client> generatedClients,
        DateTime firstTradingDay,
        DateTime lastTradingDay,
        IReadOnlyList<string> availableCurrencies);
}
