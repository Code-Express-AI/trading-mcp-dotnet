namespace TradingDataGenerator;

public interface IDataLoader
{
    Task<int> LoadAsync(string[] commandLineArgs, string databaseConnectionString);
}
