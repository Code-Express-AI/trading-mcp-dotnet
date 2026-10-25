namespace TradingMcp.Model;

public sealed record Account(
    Guid AccountKey,
    string AccountNumber,
    string ClientName,
    string Country,
    string RiskProfile,
    string AccountType,
    string BaseCurrency,
    DateTime OpenedOn);
