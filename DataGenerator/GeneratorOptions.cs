using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using TradingDataGenerator.Generation;

namespace TradingDataGenerator;

internal sealed record GeneratorOptions
{
    private static readonly DateTime EarliestEndDate = new(2000, 1, 1);

    internal static readonly string[] KnownOptions =
        ["reset", "seed", "instruments", "clients", "years", "trades-per-account", "end-date", "help"];

    public const string HelpText = """
        Generates synthetic trading data and loads it into the TradingDemo database.

        Usage:
          dotnet run -- [options]                          Docker setup: uses SA_PASSWORD from the .env file
          dotnet run -- "<connection string>" [options]    Any other SQL Server

        For any other SQL Server, the connection string can also be set in the TRADING_DB_CONNECTION
        environment variable. Connect as sa (or another admin), not as mcp_reader.

        Options:
          --reset                     Delete the existing data before loading. The audit log is kept.
          --seed=<number>             Random seed, 0 or more. Default: 42.
          --instruments=<number>      Companies to create, 1-375. Default: 50.
          --clients=<number>          Clients to create, 1-10000. Default: 200.
          --years=<number>            Years of price history, 1-10. Default: 3.
          --trades-per-account=<n>    Average trades per account, 2-1000. Default: 40.
          --end-date=<yyyy-MM-dd>     Last day of data, 2000-01-01 to yesterday. Default: yesterday (UTC).
                                      The same seed and end date always produce the same data.
          --help, -h                  Show this help.

        Examples:
          dotnet run -- --reset
          dotnet run -- "Server=myserver;Database=TradingDemo;User ID=myadmin;Password=<password>;Encrypt=True" --reset
        """;

    public required string ConnectionString { get; init; }
    public bool Reset { get; init; }
    public int Seed { get; init; }
    public int InstrumentCount { get; init; }
    public int ClientCount { get; init; }
    public int Years { get; init; }
    public int TradesPerAccount { get; init; }
    public DateTime EndDate { get; init; }

    public static GeneratorOptions Parse(string[] args, string? environmentConnectionString, DateTime today, string currentDirectory)
    {
        var namedOptions = ParseNamedOptions(args.Where(IsNamedOption));
        var positionalArguments = args.Where(arg => !IsNamedOption(arg)).ToList();
        if (positionalArguments.Count > 1)
        {
            throw new InvalidOptionsException(
                $"Unexpected argument '{positionalArguments[1]}'. Write options as --name=value, for example --seed=42.");
        }

        return new GeneratorOptions
        {
            Reset = ParseFlag(namedOptions, "reset"),
            Seed = ParseInteger(namedOptions, "seed", defaultValue: 42, minimum: 0, maximum: int.MaxValue),
            InstrumentCount = ParseInteger(namedOptions, "instruments", defaultValue: 50, minimum: 1, maximum: SeedData.MaxInstruments),
            ClientCount = ParseInteger(namedOptions, "clients", defaultValue: 200, minimum: 1, maximum: 10_000),
            Years = ParseInteger(namedOptions, "years", defaultValue: 3, minimum: 1, maximum: 10),
            TradesPerAccount = ParseInteger(namedOptions, "trades-per-account", defaultValue: 40, minimum: 2, maximum: 1_000),
            EndDate = ParseEndDate(namedOptions, today),
            // Last, so mistakes in the other options are reported before looking for a .env file.
            ConnectionString = ResolveConnectionString(positionalArguments.FirstOrDefault(), environmentConnectionString, currentDirectory)
        };
    }

    public static bool IsHelpRequested(string[] args) => args.Contains("--help") || args.Contains("-h");

    private static bool IsNamedOption(string arg) => arg.StartsWith("--", StringComparison.Ordinal);

    private static Dictionary<string, string?> ParseNamedOptions(IEnumerable<string> args)
    {
        var options = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var arg in args)
        {
            var parts = arg[2..].Split('=', 2);
            var name = parts[0];
            if (!KnownOptions.Contains(name))
            {
                throw new InvalidOptionsException($"Unknown option '--{name}'. Run with --help to see the valid options.");
            }

            options[name] = parts.Length > 1 ? parts[1] : null;
        }

        return options;
    }

    // An argument wins, then the TRADING_DB_CONNECTION environment variable, then the Docker setup's .env file.
    private static string ResolveConnectionString(string? argument, string? environmentConnectionString, string currentDirectory)
    {
        var connectionString = !string.IsNullOrWhiteSpace(argument) ? argument
            : !string.IsNullOrWhiteSpace(environmentConnectionString) ? environmentConnectionString
            : EnvFile.BuildDockerConnectionString(currentDirectory);

        if (Regex.IsMatch(connectionString, "<[A-Za-z_]+>"))
        {
            throw new InvalidOptionsException(
                "The connection string still contains a placeholder such as <SA_PASSWORD>. " +
                "Replace it, including the angle brackets, with the value from your .env file.");
        }

        try
        {
            _ = new SqlConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidOptionsException($"The connection string isn't valid: {exception.Message}");
        }

        return connectionString;
    }

    private static bool ParseFlag(Dictionary<string, string?> options, string name)
    {
        if (!options.TryGetValue(name, out var value))
        {
            return false;
        }

        if (value is not null)
        {
            throw new InvalidOptionsException($"--{name} doesn't take a value. Use --{name} on its own.");
        }

        return true;
    }

    private static int ParseInteger(
        Dictionary<string, string?> options, string name, int defaultValue, int minimum, int maximum)
    {
        if (!options.TryGetValue(name, out var rawValue))
        {
            return defaultValue;
        }

        if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidOptionsException(
                $"Invalid --{name} value '{rawValue}'. Expected a whole number, for example --{name}={defaultValue}.");
        }

        if (value < minimum || value > maximum)
        {
            throw new InvalidOptionsException($"--{name} must be between {minimum} and {maximum}.");
        }

        return value;
    }

    private static DateTime ParseEndDate(Dictionary<string, string?> options, DateTime today)
    {
        var yesterday = today.Date.AddDays(-1);
        if (!options.TryGetValue("end-date", out var rawValue))
        {
            return yesterday;
        }

        if (!DateTime.TryParseExact(rawValue, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
        {
            throw new InvalidOptionsException(
                $"Invalid --end-date value '{rawValue}'. Expected yyyy-MM-dd, for example --end-date=2026-01-30.");
        }

        if (endDate < EarliestEndDate || endDate > yesterday)
        {
            throw new InvalidOptionsException(
                $"--end-date must be between {EarliestEndDate:yyyy-MM-dd} and yesterday ({yesterday:yyyy-MM-dd}).");
        }

        return endDate;
    }
}
