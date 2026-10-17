using TradingDataGenerator.Generation.Generators;

namespace XTests.DataGenerator;

public class When_Clients_Are_Generated
{
    [Fact]
    public void it_should_generate_repeatable_clients_for_the_same_seed()
    {
        ClientGenerator clientGenerator;
        DateTime firstTradingDay;
        List<TradingDataGenerator.Models.Client> firstGeneratedClients;
        List<TradingDataGenerator.Models.Client> secondGeneratedClients;

        clientGenerator = new ClientGenerator();
        firstTradingDay = new DateTime(2025, 1, 1);
        firstGeneratedClients = clientGenerator.GenerateClients(1234, 10, firstTradingDay);
        secondGeneratedClients = clientGenerator.GenerateClients(1234, 10, firstTradingDay);

        Assert.Equal(firstGeneratedClients, secondGeneratedClients);
        Assert.All(firstGeneratedClients, client =>
        {
            Assert.EndsWith("@example.com", client.Email);
            Assert.InRange(client.CreatedOn, firstTradingDay.AddDays(-1500), firstTradingDay.AddDays(-30));
        });
    }
}
