namespace TradingMcp.Model;

public sealed class Account
{
    public Account(
        int accountId,
        string accountNumber,
        string clientName,
        string country,
        string riskProfile,
        string accountType,
        string baseCurrency,
        DateTime openedOn)
    {
        AccountId = accountId;
        AccountNumber = accountNumber;
        ClientName = clientName;
        Country = country;
        RiskProfile = riskProfile;
        AccountType = accountType;
        BaseCurrency = baseCurrency;
        OpenedOn = openedOn;
    }

    public int AccountId { get; }
    public string AccountNumber { get; }
    public string ClientName { get; }
    public string Country { get; }
    public string RiskProfile { get; }
    public string AccountType { get; }
    public string BaseCurrency { get; }
    public DateTime OpenedOn { get; }
}
