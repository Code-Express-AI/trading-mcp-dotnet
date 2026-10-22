namespace TradingMcp;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        WebApplication application;
        try
        {
            application = McpServerApplication.Build(args);
        }
        catch (ServerConfigurationException exception)
        {
            await Console.Error.WriteLineAsync(exception.Message);
            return 1;
        }

        await StartupDatabaseCheck.RunAsync(application);
        await application.RunAsync();
        return 0;
    }
}
