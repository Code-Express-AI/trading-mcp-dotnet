using System.Text.RegularExpressions;
using Bogus;
using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation;

// Bogus supplies realistic UK names. It has its own seeded random source, so names stay the same
// for a given seed even if the other generators change.
internal static class ClientGenerator
{
    public static List<Client> Generate(int seed, int count, DateTime firstTradingDay)
    {
        var faker = new Faker("en_GB") { Random = new Randomizer(seed) };
        return Enumerable.Range(1, count).Select(id => CreateClient(faker, id, firstTradingDay)).ToList();
    }

    private static Client CreateClient(Faker faker, int id, DateTime firstTradingDay)
    {
        var firstName = faker.Name.FirstName();
        var lastName = faker.Name.LastName();
        var emailName = Regex.Replace($"{firstName}.{lastName}".ToLowerInvariant(), "[^a-z.]", "");

        return new Client(
            Id: id,
            FullName: $"{firstName} {lastName}",
            Email: $"{emailName}@example.com",
            Country: faker.PickRandom<string>(SeedData.Countries),
            RiskProfile: faker.PickRandom<string>(SeedData.RiskProfiles),
            // Clients joined between a month and about four years before the data starts.
            CreatedOn: firstTradingDay.AddDays(-faker.Random.Int(30, 1500)));
    }
}
