using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace TradingMcp;

// Runs one stored procedure at startup, so a wrong password or a missing database shows up in the console
// straight away instead of as a failed tool call in the chat. It only warns: the server still starts.
internal static class StartupDatabaseCheck
{
    public static async Task RunAsync(WebApplication application)
    {
        var connectionString = application.Configuration.GetConnectionString("Trading")!;
        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.ExecuteAsync("mcp.GetDatasetInfo", commandType: CommandType.StoredProcedure);
            application.Logger.LogInformation("Database check passed.");
        }
        catch (SqlException exception)
        {
            application.Logger.LogWarning(
                "Database check failed. {Problem} The server will start, but tool calls will fail until this is fixed.",
                Describe(exception.Number, exception.Message, connectionString));
        }
    }

    internal static string Describe(int errorNumber, string errorMessage, string connectionString)
    {
        var connection = new SqlConnectionStringBuilder(connectionString);
        return errorNumber switch
        {
            18456 => $"SQL Server rejected the login for '{connection.UserID}'. Check the password matches MCP_READER_PASSWORD in your .env file.",
            4060 => $"The database '{connection.InitialCatalog}' doesn't exist. Run: docker compose run --rm db-init",
            2812 => "The mcp stored procedures don't exist. Run: docker compose run --rm db-init",
            229 => $"'{connection.UserID}' isn't allowed to run the mcp stored procedures. Run: docker compose run --rm db-init",
            // 0 is how macOS and Linux report a failed connection; the others are the Windows equivalents.
            0 or -2 or 2 or 53 or 258 or 10060 or 10061 or 11001 =>
                $"Couldn't connect to SQL Server at {connection.DataSource}. Check the container is running: docker compose ps",
            _ => errorMessage
        };
    }
}
