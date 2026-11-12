using TradingDataGenerator;
using TradingDataGenerator.Generation;

namespace XTests.DataGenerator;

// The docs and videos search for "Thompson". Whatever the seed, or the version of Bogus that picks the other names,
// a search for "Thompson" must find at least four accounts, with any token.
public class When_Example_Clients_Are_Generated
{
    private static GeneratorOptions OptionsFor(int seed) => GeneratorOptions.Parse(
        [$"--seed={seed}", "--years=1", "--instruments=5", "--trades-per-account=2", "--end-date=2025-12-31"],
        "Server=localhost;Database=TradingDemo",
        new DateTime(2026, 3, 15),
        Path.GetTempPath());

    [Theory]
    [InlineData(42)]
    [InlineData(7)]
    [InlineData(123456)]
    public void it_should_always_include_giovanny_and_tessie_thompson(int seed)
    {
        var clients = SyntheticData.Generate(OptionsFor(seed)).Clients;

        Assert.Equal("Giovanny Thompson", clients.Single(client => client.Id == 34).FullName);
        Assert.Equal("Tessie Thompson", clients.Single(client => client.Id == 53).FullName);
    }

    [Theory]
    [InlineData(42)]
    [InlineData(7)]
    [InlineData(123456)]
    public void it_should_give_every_thompson_at_least_two_accounts_that_every_token_can_see(int seed)
    {
        var data = SyntheticData.Generate(OptionsFor(seed));
        var thompsons = data.Clients.Where(client => client.FullName.Contains("Thompson")).ToList();

        Assert.True(data.Accounts.Count(account => thompsons.Any(client => client.Id == account.ClientId)) >= 4);
        Assert.All(thompsons, thompson =>
        {
            Assert.True(data.Accounts.Count(account => account.ClientId == thompson.Id) >= 2);
            Assert.Equal(
                ["claude", "copilot", "inspector", "local"],
                data.CallerClients.Where(row => row.ClientId == thompson.Id).Select(row => row.CallerKey).Order());
        });
    }
}
