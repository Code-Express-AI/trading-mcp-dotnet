namespace TradingDataGenerator.Models;

public record Client(
    int Id,
    string FullName,
    string Email,
    string Country,
    string RiskProfile,
    DateTime CreatedOn);
