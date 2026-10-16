using System.Text.RegularExpressions;
using Bogus;
using TradingDataGenerator.Generation.Builders;
using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation.Generators;

public sealed class ClientGenerator : IClientGenerator
{
    public List<Client> GenerateClients(int randomSeed, int clientCount, DateTime firstTradingDay)
    {
        Faker clientFaker;
        string[] availableCountries;
        string[] availableRiskProfiles;
        List<Client> generatedClientsToReturn;
        int clientId;
        string firstName;
        string lastName;
        string         emailLocalPart;

        clientFaker = new Faker("en_GB") { Random = new Randomizer(randomSeed) };
        availableCountries = SeedData.Countries.ToArray();
        availableRiskProfiles = SeedData.RiskProfiles.ToArray();
        generatedClientsToReturn = new List<Client>(clientCount);
        for (clientId = 1; clientId <= clientCount; clientId++)
        {
            firstName = clientFaker.Name.FirstName();
            lastName = clientFaker.Name.LastName();
            emailLocalPart = Regex.Replace($"{firstName}.{lastName}".ToLowerInvariant(), "[^a-z.]", "");

            generatedClientsToReturn.Add(new ClientBuilder()
                .WithId(clientId)
                .WithFullName($"{firstName} {lastName}")
                .WithEmail($"{emailLocalPart}@example.com")
                .WithCountry(clientFaker.Random.ArrayElement(availableCountries))
                .WithRiskProfile(clientFaker.Random.ArrayElement(availableRiskProfiles))
                .WithCreatedOn(firstTradingDay.AddDays(-clientFaker.Random.Int(30, 1500)))
                .Build());
        }
        return generatedClientsToReturn;
    }
}
