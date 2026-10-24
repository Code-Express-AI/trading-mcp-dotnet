using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace XTests.Integration;

// The local Docker SQL Server, using the passwords in the repository's .env file.
// If SQL Server isn't running or has no data, the integration tests are skipped with the reason, not failed,
// so `dotnet test` still passes on a machine without Docker. On CI, When_Ci_Build_Is_Run fails instead.
internal static class IntegrationDatabase
{
    private static readonly Lazy<string> RepositoryRoot = new(FindRepositoryRoot);
    private static readonly Lazy<Dictionary<string, string>?> EnvValues = new(ReadEnvFile);
    private static readonly Lazy<string?> UnavailableReason = new(CheckAvailability);

    public static string? SkipReason => UnavailableReason.Value;

    public const string DatabaseName = "TradingDemo";

    public static string AdminConnectionString(string? database = null) =>
        Build("sa", EnvValues.Value!["SA_PASSWORD"], database ?? DatabaseName);

    public static string ReaderConnectionString => Build("mcp_reader", EnvValues.Value!["MCP_READER_PASSWORD"], DatabaseName);

    public static string Build(string user, string password, string database, bool trustServerCertificate = true) =>
        new SqlConnectionStringBuilder
        {
            DataSource = "localhost,1433",
            InitialCatalog = database,
            UserID = user,
            Password = password,
            Encrypt = true,
            TrustServerCertificate = trustServerCertificate,
            ConnectTimeout = 3
        }.ConnectionString;

    public static async Task<T> QueryScalarAsync<T>(string sql, string? database = null)
    {
        await using var connection = new SqlConnection(AdminConnectionString(database));
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    public static async Task ExecuteAsync(string sql, string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    // Runs a script from the sql folder. "GO" is a tool convention, not T-SQL, so the script is split on it.
    public static async Task RunScriptAsync(string scriptName, string connectionString)
    {
        var script = await File.ReadAllTextAsync(Path.Combine(RepositoryRoot.Value, "sql", scriptName));
        foreach (var batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(batch))
            {
                await ExecuteAsync(batch, connectionString);
            }
        }
    }

    private static string? CheckAvailability()
    {
        if (EnvValues.Value is null)
        {
            return "No .env file, so there's no local SQL Server to test against. See docs/01-docker-database.md.";
        }

        try
        {
            using var connection = new SqlConnection(AdminConnectionString());
            connection.Open();
            using var command = new SqlCommand("SELECT COUNT(*) FROM trading.Instrument", connection);
            return (int)command.ExecuteScalar()! > 0
                ? null
                : "The database has no data. Load it with: dotnet run --project DataGenerator -- --reset";
        }
        catch (SqlException exception)
        {
            return $"SQL Server isn't available, so the integration tests were skipped. Is Docker running? Start SQL Server with: docker compose up -d --wait ({exception.Message})";
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "docker-compose.yml")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException("Couldn't find the repository root (the folder containing docker-compose.yml).");
    }

    private static Dictionary<string, string>? ReadEnvFile()
    {
        var path = Path.Combine(RepositoryRoot.Value, ".env");
        if (!File.Exists(path))
        {
            return null;
        }

        return File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#') && line.Contains('='))
            .Select(line => line.Split('=', 2))
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim().Trim('"', '\''));
    }
}

// A [Fact] that's skipped, with the reason, when the local SQL Server isn't available.
public sealed class IntegrationFactAttribute : FactAttribute
{
    public IntegrationFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        Skip = IntegrationDatabase.SkipReason;
    }
}
