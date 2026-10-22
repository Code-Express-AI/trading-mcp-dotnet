using Microsoft.Data.SqlClient;

namespace TradingDataGenerator;

// The Docker setup keeps its passwords in the .env file that docker compose reads. Using the same file here
// means the Docker walkthrough needs no connection string, and the password never appears on the command line.
internal static class EnvFile
{
    private const string FileName = ".env";

    public static string BuildDockerConnectionString(string startDirectory)
    {
        var path = Find(startDirectory) ?? throw new EnvFileNotFoundException(startDirectory);
        var values = Read(path);

        if (!values.TryGetValue("SA_PASSWORD", out var password) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOptionsException($"{path} doesn't set SA_PASSWORD. Copy the line from .env.example and set a password.");
        }

        Console.WriteLine($"Using SA_PASSWORD from {path}");
        return new SqlConnectionStringBuilder
        {
            DataSource = "localhost,1433",
            InitialCatalog = values.GetValueOrDefault("DB_NAME") is { Length: > 0 } database ? database : "TradingDemo",
            UserID = "sa",
            Password = password,
            Encrypt = true,
            TrustServerCertificate = true,
            ConnectTimeout = 5
        }.ConnectionString;
    }

    // Searches upwards, so it works from the repository root or from the DataGenerator folder.
    private static string? Find(string startDirectory)
    {
        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, FileName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    // KEY=VALUE lines. Blank lines and # comments are skipped, and surrounding quotes are removed.
    private static Dictionary<string, string> Read(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#') || !line.Contains('='))
            {
                continue;
            }

            var parts = line.Split('=', 2);
            values[parts[0].Trim()] = parts[1].Trim().Trim('"', '\'');
        }

        return values;
    }
}
