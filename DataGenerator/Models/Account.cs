namespace TradingDataGenerator.Models;

public record Account(
    int Id,
    int ClientId,
    string AccountNumber,
    string AccountType,
    string BaseCurrency,
    DateTime OpenedOn);
