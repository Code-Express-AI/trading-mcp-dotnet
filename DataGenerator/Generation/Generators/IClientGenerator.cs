using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation.Generators;

public interface IClientGenerator
{
    List<Client> GenerateClients(int randomSeed, int clientCount, DateTime firstTradingDay);
}
