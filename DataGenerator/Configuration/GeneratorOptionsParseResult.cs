namespace TradingDataGenerator.Configuration;

public sealed class GeneratorOptionsParseResult
{
    private GeneratorOptionsParseResult(GeneratorOptions? options, string? errorMessage)
    {
        Options = options;
        ErrorMessage = errorMessage;
    }

    public GeneratorOptions? Options { get; }
    public string? ErrorMessage { get; }

    internal static GeneratorOptionsParseResult Success(GeneratorOptions options)
    {
        return new GeneratorOptionsParseResult(options, null);
    }

    internal static GeneratorOptionsParseResult Failure(string errorMessage)
    {
        return new GeneratorOptionsParseResult(null, errorMessage);
    }
}
