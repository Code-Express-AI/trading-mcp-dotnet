using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation.Builders;

public sealed class ClientBuilder
{
    private int? _id;
    private string? _fullName;
    private string? _email;
    private string? _country;
    private string? _riskProfile;
    private DateTime? _createdOn;

    public ClientBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public ClientBuilder WithFullName(string fullName)
    {
        _fullName = fullName ?? throw new ArgumentNullException(nameof(fullName));
        return this;
    }

    public ClientBuilder WithEmail(string email)
    {
        _email = email ?? throw new ArgumentNullException(nameof(email));
        return this;
    }

    public ClientBuilder WithCountry(string country)
    {
        _country = country ?? throw new ArgumentNullException(nameof(country));
        return this;
    }

    public ClientBuilder WithRiskProfile(string riskProfile)
    {
        _riskProfile = riskProfile ?? throw new ArgumentNullException(nameof(riskProfile));
        return this;
    }

    public ClientBuilder WithCreatedOn(DateTime createdOn)
    {
        _createdOn = createdOn;
        return this;
    }

    public Client Build()
    {
        int id;
        string fullName;
        string email;
        string country;
        string riskProfile;
        DateTime createdOn;
        Client clientToReturn;

        id = _id ?? throw new InvalidOperationException("Client ID has not been provided.");
        fullName = _fullName ?? throw new InvalidOperationException("Client full name has not been provided.");
        email = _email ?? throw new InvalidOperationException("Client email has not been provided.");
        country = _country ?? throw new InvalidOperationException("Client country has not been provided.");
        riskProfile = _riskProfile ?? throw new InvalidOperationException("Client risk profile has not been provided.");
        createdOn = _createdOn ?? throw new InvalidOperationException("Client creation date has not been provided.");

        clientToReturn = new Client(id, fullName, email, country, riskProfile, createdOn);
        return clientToReturn;
    }
}
