using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation.Builders;

public sealed class AccountBuilder
{
    private int? _id;
    private int? _clientId;
    private string? _accountNumber;
    private string? _accountType;
    private string? _baseCurrency;
    private DateTime? _openedOn;

    public AccountBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public AccountBuilder WithClientId(int clientId)
    {
        _clientId = clientId;
        return this;
    }

    public AccountBuilder WithAccountNumber(string accountNumber)
    {
        _accountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
        return this;
    }

    public AccountBuilder WithAccountType(string accountType)
    {
        _accountType = accountType ?? throw new ArgumentNullException(nameof(accountType));
        return this;
    }

    public AccountBuilder WithBaseCurrency(string baseCurrency)
    {
        _baseCurrency = baseCurrency ?? throw new ArgumentNullException(nameof(baseCurrency));
        return this;
    }

    public AccountBuilder WithOpenedOn(DateTime openedOn)
    {
        _openedOn = openedOn;
        return this;
    }

    public Account Build()
    {
        int id;
        int clientId;
        string accountNumber;
        string accountType;
        string baseCurrency;
        DateTime openedOn;
        Account accountToReturn;

        id = _id ?? throw new InvalidOperationException("Account ID has not been provided.");
        clientId = _clientId ?? throw new InvalidOperationException("Account client ID has not been provided.");
        accountNumber = _accountNumber ?? throw new InvalidOperationException("Account number has not been provided.");
        accountType = _accountType ?? throw new InvalidOperationException("Account type has not been provided.");
        baseCurrency = _baseCurrency ?? throw new InvalidOperationException("Account base currency has not been provided.");
        openedOn = _openedOn ?? throw new InvalidOperationException("Account open date has not been provided.");

        accountToReturn = new Account(id, clientId, accountNumber, accountType, baseCurrency, openedOn);
        return accountToReturn;
    }
}
