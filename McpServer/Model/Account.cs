namespace TradingMcp.Model;

public sealed record Account(
    int AccountId,
    string AccountNumber,
    string ClientName,
    string Country,
    string RiskProfile,
    string AccountType,
    string BaseCurrency,
    DateTime OpenedOn);
