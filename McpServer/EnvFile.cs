using Microsoft.Data.SqlClient;

namespace TradingMcp;

// In Development, the server can use the Docker setup's .env file, so the local walkthrough needs no
// user-secrets. Production never reads .env.
internal static class EnvFile
{
    private const string FileName = ".env";

    // Searches upwards, so it finds the .env in the repository root from the McpServer folder.
    public static IReadOnlyDictionary<string, string>? TryRead(string startDirectory, out string? path)
    {
        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            path = Path.Combine(directory.FullName, FileName);
            if (File.Exists(path))
            {
                return Read(path);
            }
        }

        path = null;
        return null;
    }

    public static string BuildReaderConnectionString(string password) =>
        new SqlConnectionStringBuilder
        {
            DataSource = "localhost,1433",
            InitialCatalog = "TradingDemo",
            UserID = "mcp_reader",
            Password = password,
            Encrypt = true,
            TrustServerCertificate = true,
            ConnectTimeout = 5
        }.ConnectionString;

    // KEY=VALUE lines. Blank lines and # comments are skipped, and surrounding quotes are removed.
    private static Dictionary<string, string> Read(string path) =>
        File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#') && line.Contains('='))
            .Select(line => line.Split('=', 2))
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim().Trim('"', '\''));
}
