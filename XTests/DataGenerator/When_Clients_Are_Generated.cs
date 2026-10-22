using TradingDataGenerator.Generation;

namespace XTests.DataGenerator;

public class When_Clients_Are_Generated
{
    private static readonly DateTime FirstTradingDay = new(2025, 1, 1);

    [Fact]
    public void it_should_generate_the_same_clients_for_the_same_seed()
    {
        var first = ClientGenerator.Generate(1234, 10, FirstTradingDay);
        var second = ClientGenerator.Generate(1234, 10, FirstTradingDay);

        Assert.Equal(first, second);
    }

    [Fact]
    public void it_should_generate_safe_emails_and_join_dates_before_the_data_starts()
    {
        var clients = ClientGenerator.Generate(1234, 10, FirstTradingDay);

        Assert.Equal(Enumerable.Range(1, 10), clients.Select(client => client.Id));
        Assert.All(clients, client =>
        {
            Assert.EndsWith("@example.com", client.Email);
            Assert.InRange(client.CreatedOn, FirstTradingDay.AddDays(-1500), FirstTradingDay.AddDays(-30));
        });
    }
}
