using TradingDataGenerator.Constants;

namespace TradingDataGenerator.Configuration;

public sealed class GeneratorOptions : IGeneratorOptions
{
    public bool Reset { get; init; }
    public int Seed { get; init; }
    public int InstrumentCount { get; init; }
    public int ClientCount { get; init; }
    public int Years { get; init; }
    public int TradesPerAccount { get; init; }

    public static GeneratorOptionsParseResult Parse(string[] commandLineArgs)
    {
        Dictionary<string, string> parsedOptions;
        IntegerOptionsParseResult integerOptionsParseResult;
        string? validationErrorMessage;
        ParsedIntegerOptions parsedIntegerOptions;
        GeneratorOptions generatorOptions;

        parsedOptions = ParseCommandLineOptions(commandLineArgs);
        integerOptionsParseResult = ParseIntegerOptions(parsedOptions);
        if (integerOptionsParseResult.ErrorMessage is not null)
        {
            return GeneratorOptionsParseResult.Failure(integerOptionsParseResult.ErrorMessage);
        }

        parsedIntegerOptions = integerOptionsParseResult.Options!;
        validationErrorMessage = Validate(parsedIntegerOptions);
        if (validationErrorMessage is not null)
        {
            return GeneratorOptionsParseResult.Failure(validationErrorMessage);
        }

        generatorOptions = new GeneratorOptions
        {
            Reset = parsedOptions.ContainsKey("reset"),
            Seed = parsedIntegerOptions.Seed,
            InstrumentCount = parsedIntegerOptions.InstrumentCount,
            ClientCount = parsedIntegerOptions.ClientCount,
            Years = parsedIntegerOptions.YearCount,
            TradesPerAccount = parsedIntegerOptions.TradesPerAccount
        };
        return GeneratorOptionsParseResult.Success(generatorOptions);
    }

    private static Dictionary<string, string> ParseCommandLineOptions(string[] commandLineArgs)
    {
        Dictionary<string, string> parsedOptions;
        string[] optionParts;

        parsedOptions = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var commandLineArgument in commandLineArgs)
        {
            if (!commandLineArgument.StartsWith("--", StringComparison.Ordinal))
            {
                continue;
            }

            optionParts = commandLineArgument[2..].Split('=', 2);
            parsedOptions[optionParts[0]] = optionParts.Length > 1 ? optionParts[1] : "true";
        }

        return parsedOptions;
    }

    private static string? Validate(ParsedIntegerOptions parsedIntegerOptions)
    {
        if (!IsValidInstrumentCount(parsedIntegerOptions.InstrumentCount))
        {
            return ErrorMessages.InvalidInstrumentCount;
        }

        if (!IsValidClientCount(parsedIntegerOptions.ClientCount))
        {
            return ErrorMessages.NegativeClientCount;
        }

        if (!IsValidYears(parsedIntegerOptions.YearCount))
        {
            return GetYearCountValidationError(parsedIntegerOptions.YearCount);
        }

        if (!IsValidTradesPerAccount(parsedIntegerOptions.TradesPerAccount))
        {
            return ErrorMessages.InvalidTradesPerAccount;
        }

        return null;
    }

    private static string GetYearCountValidationError(int yearCount)
    {
        DateTime lastAvailableTradingDate;
        string validationErrorMessage;

        if (yearCount < 1)
        {
            return ErrorMessages.InvalidYearCount;
        }

        lastAvailableTradingDate = DateTime.UtcNow.Date.AddDays(-1);
        validationErrorMessage = string.Format(
            ErrorMessages.YearsExceedAvailableRange,
            lastAvailableTradingDate.Year - 1);
        return validationErrorMessage;
    }

    private static IntegerOptionsParseResult ParseIntegerOptions(
        IReadOnlyDictionary<string, string> parsedOptions)
    {
        IntegerOptionParseResult seed;
        IntegerOptionParseResult instruments;
        IntegerOptionParseResult clients;
        IntegerOptionParseResult years;
        IntegerOptionParseResult tradesPerAccount;
        ParsedIntegerOptions parsedIntegerOptions;

        seed = ParseIntegerOption(parsedOptions, "seed", 42);
        if (seed.ErrorMessage is not null)
        {
            return new IntegerOptionsParseResult(null, seed.ErrorMessage);
        }

        instruments = ParseIntegerOption(parsedOptions, "instruments", 50);
        if (instruments.ErrorMessage is not null)
        {
            return new IntegerOptionsParseResult(null, instruments.ErrorMessage);
        }

        clients = ParseIntegerOption(parsedOptions, "clients", 200);
        if (clients.ErrorMessage is not null)
        {
            return new IntegerOptionsParseResult(null, clients.ErrorMessage);
        }

        years = ParseIntegerOption(parsedOptions, "years", 3);
        if (years.ErrorMessage is not null)
        {
            return new IntegerOptionsParseResult(null, years.ErrorMessage);
        }

        tradesPerAccount = ParseIntegerOption(parsedOptions, "trades-per-account", 40);
        if (tradesPerAccount.ErrorMessage is not null)
        {
            return new IntegerOptionsParseResult(null, tradesPerAccount.ErrorMessage);
        }

        parsedIntegerOptions = new ParsedIntegerOptions
        {
            Seed = seed.Value,
            InstrumentCount = instruments.Value,
            ClientCount = clients.Value,
            YearCount = years.Value,
            TradesPerAccount = tradesPerAccount.Value
        };
        return new IntegerOptionsParseResult(parsedIntegerOptions, null);
    }

    private static IntegerOptionParseResult ParseIntegerOption(
        IReadOnlyDictionary<string, string> parsedOptions,
        string optionName,
        int fallbackValue)
    {
        int parsedValue;
        string? rawOptionValue;
        string validationErrorMessage;

        if (!parsedOptions.TryGetValue(optionName, out rawOptionValue))
        {
            return new IntegerOptionParseResult(fallbackValue, null);
        }

        if (int.TryParse(rawOptionValue, out parsedValue))
        {
            return new IntegerOptionParseResult(parsedValue, null);
        }

        validationErrorMessage = string.Format(
            GetInvalidIntegerOptionMessage(optionName),
            rawOptionValue);
        return new IntegerOptionParseResult(default, validationErrorMessage);
    }

    private static bool IsValidInstrumentCount(int instrumentCount)
    {
        bool isValidInstrumentCount;
        isValidInstrumentCount = instrumentCount >= 1;
        return isValidInstrumentCount;
    }

    private static bool IsValidClientCount(int clientCount)
    {
        bool isValidClientCount;
        isValidClientCount = clientCount >= 0;
        return isValidClientCount;
    }

    private static bool IsValidYears(int yearCount)
    {
        bool isValidYearCount;
        DateTime lastAvailableTradingDate;
        lastAvailableTradingDate = DateTime.UtcNow.Date.AddDays(-1);
        isValidYearCount = yearCount >= 1 && yearCount <= lastAvailableTradingDate.Year - 1;
        return isValidYearCount;
    }

    private static bool IsValidTradesPerAccount(int tradesPerAccount)
    {
        bool isValidTradesPerAccount;
        isValidTradesPerAccount = tradesPerAccount >= 2 && tradesPerAccount <= int.MaxValue / 2;
        return isValidTradesPerAccount;
    }

    private static string GetInvalidIntegerOptionMessage(string optionName)
    {
        string invalidIntegerOptionMessage;

        invalidIntegerOptionMessage = optionName switch
        {
            "seed" => ErrorMessages.InvalidSeed,
            "instruments" => ErrorMessages.InvalidInstrumentCountFormat,
            "clients" => ErrorMessages.InvalidClientCountFormat,
            "years" => ErrorMessages.InvalidYearsFormat,
            "trades-per-account" => ErrorMessages.InvalidTradesPerAccountFormat,
            _ => throw new ArgumentOutOfRangeException(nameof(optionName), optionName, "Unknown integer option.")
        };
        return invalidIntegerOptionMessage;
    }

    private sealed record ParsedIntegerOptions
    {
        public int Seed { get; init; }
        public int InstrumentCount { get; init; }
        public int ClientCount { get; init; }
        public int YearCount { get; init; }
        public int TradesPerAccount { get; init; }
    }

    private sealed record IntegerOptionParseResult(int Value, string? ErrorMessage);

    private sealed record IntegerOptionsParseResult(ParsedIntegerOptions? Options, string? ErrorMessage);
}
