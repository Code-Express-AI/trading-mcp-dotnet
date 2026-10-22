namespace TradingDataGenerator.Models;

internal sealed record Account(
    int Id,
    int ClientId,
    string AccountNumber,
    string AccountType,
    string BaseCurrency,
    DateTime OpenedOn);
