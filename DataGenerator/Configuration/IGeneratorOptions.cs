namespace TradingDataGenerator.Configuration;

public interface IGeneratorOptions
{
    bool Reset { get; }
    int Seed { get; }
    int InstrumentCount { get; }
    int ClientCount { get; }
    int Years { get; }
    int TradesPerAccount { get; }
}
