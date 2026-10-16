using TradingDataGenerator.Constants;

namespace TradingDataGenerator;

internal sealed class Program
{
    private static async Task<int> Main(string[] commandLineArgs)
    {
        string databaseConnectionString;
        IDataLoader dataLoader;
        int loadResultCode;

        databaseConnectionString = GetDatabaseConnectionString(commandLineArgs);
        if (!IsConnectionStringValid(databaseConnectionString))
        {
            Console.Error.WriteLine(ErrorMessages.MissingConnectionString);
            return ExitCodes.InvalidConfiguration;
        }

        dataLoader = new DataLoader();
        loadResultCode = await dataLoader.LoadAsync(commandLineArgs, databaseConnectionString);
        return loadResultCode;
    }

    private static string GetDatabaseConnectionString(string[] commandLineArgs)
    {
        string databaseConnectionString;

        databaseConnectionString = commandLineArgs.FirstOrDefault(commandLineArgument =>
        {
            return !commandLineArgument.StartsWith("--", StringComparison.Ordinal);
        }) ?? Environment.GetEnvironmentVariable("TRADING_DB_CONNECTION") ?? string.Empty;
        return databaseConnectionString;
    }

    private static bool IsConnectionStringValid(string? databaseConnectionString)
    {
        bool isConnectionStringValid;
        isConnectionStringValid = !string.IsNullOrWhiteSpace(databaseConnectionString);
        return isConnectionStringValid;
    }
}
