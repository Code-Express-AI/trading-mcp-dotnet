using TradingDataGenerator.Configuration;
using TradingDataGenerator.Generation;
using TradingDataGenerator.Generation.Generators;
using TradingDataGenerator.Models;

namespace XTests.DataGenerator;

public class When_Generated_Data_Is_Prepared
{
    [Fact]
    public void it_should_prepare_related_generated_collections()
    {
        GeneratorOptionsParseResult optionsParseResult;
        GeneratorOptions generatorOptions;
        DataPreparation dataPreparation;
        GeneratedData generatedData;

        optionsParseResult = GeneratorOptions.Parse(
            ["--years=1", "--instruments=1", "--clients=1", "--trades-per-account=2"]);
        generatorOptions = optionsParseResult.Options
            ?? throw new InvalidOperationException(optionsParseResult.ErrorMessage);
        dataPreparation = new DataPreparation(new SyntheticDataGenerator());

        generatedData = dataPreparation.PrepareGeneratedData(generatorOptions);

        Assert.Single(generatedData.Instruments);
        Assert.NotEmpty(generatedData.Prices);
        Assert.Single(generatedData.Clients);
        Assert.NotEmpty(generatedData.Accounts);
        Assert.NotEmpty(generatedData.Trades);
        Assert.All(generatedData.Trades, trade =>
        {
            Assert.Contains(generatedData.Accounts, account => account.Id == trade.AccountId);
            Assert.Contains(generatedData.Instruments, instrument => instrument.Id == trade.InstrumentId);
        });
    }
}
