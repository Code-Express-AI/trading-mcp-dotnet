namespace TradingDataGenerator;

internal sealed class EnvFileNotFoundException(string startDirectory) : InvalidOptionsException(string.Join(Environment.NewLine,
    $"No connection string, and no .env file found in {startDirectory} or any folder above it.",
    "- Docker setup: copy .env.example to .env in the repository root and set the passwords.",
    "- Your own SQL Server: pass the connection string as the first argument, or set TRADING_DB_CONNECTION."));
