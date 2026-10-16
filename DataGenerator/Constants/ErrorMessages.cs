namespace TradingDataGenerator.Constants;

internal static class ErrorMessages
{
    public static string MissingConnectionString => "No connection string. Pass it as the first argument or set TRADING_DB_CONNECTION.";
    public static string DatabaseAlreadyContainsData =>
        "The database already contains data. Re-run with --reset to wipe and regenerate it.";
    public static string InvalidInstrumentCount => "--instruments must be greater than zero.";
    public static string NegativeClientCount => "--clients cannot be negative.";
    public static string InvalidYearCount => "--years must be greater than zero.";
    public static string InvalidTradesPerAccount =>
        $"--trades-per-account must be between 2 and {int.MaxValue / 2}.";
    public static string InvalidSeed => "Invalid --seed value '{0}'. Expected an integer.";
    public static string InvalidInstrumentCountFormat =>
        "Invalid --instruments value '{0}'. Expected an integer.";
    public static string InvalidClientCountFormat => "Invalid --clients value '{0}'. Expected an integer.";
    public static string InvalidYearsFormat => "Invalid --years value '{0}'. Expected an integer.";
    public static string InvalidTradesPerAccountFormat =>
        "Invalid --trades-per-account value '{0}'. Expected an integer.";
    public static string YearsExceedAvailableRange =>
        "--years cannot exceed {0} for the current date range.";
}
