namespace TradingDataGenerator.Models;

internal sealed record Client(
    int Id,
    string FullName,
    string Email,
    string Country,
    string RiskProfile,
    DateTime CreatedOn);
