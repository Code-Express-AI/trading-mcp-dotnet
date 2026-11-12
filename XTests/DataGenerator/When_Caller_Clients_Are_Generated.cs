using TradingDataGenerator.Generation;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Caller_Clients_Are_Generated
{
    private static Client ClientCalled(int id, string fullName) => new(id, fullName, "x@example.com", "Ireland", "Balanced", new DateTime(2024, 1, 1));

    private static readonly Client[] Clients =
    [
        ClientCalled(1, "Einar Bernier"),   // copilot's quarter
        ClientCalled(2, "Milan Bernier"),   // claude's quarter
        ClientCalled(3, "Ann Lee"),         // inspector's quarter
        ClientCalled(4, "Bo Kim"),          // the shared quarter
        ClientCalled(5, "Tessie Thompson")  // copilot's quarter by number, but every Thompson is shared
    ];

    private static int[] ClientsOf(string callerKey) =>
        [.. CallerClientGenerator.Generate(Clients).Where(row => row.CallerKey == callerKey).Select(row => row.ClientId)];

    [Fact]
    public void it_should_give_the_local_token_every_client()
    {
        Assert.Equal([1, 2, 3, 4, 5], ClientsOf("local"));
    }

    [Fact]
    public void it_should_give_each_named_token_its_own_quarter_plus_the_shared_one_and_every_thompson()
    {
        Assert.Equal([1, 4, 5], ClientsOf("copilot"));
        Assert.Equal([2, 4, 5], ClientsOf("claude"));
        Assert.Equal([3, 4, 5], ClientsOf("inspector"));
    }
}
