using TradingDataGenerator.Configuration;
using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation;

public sealed class DataPreparation : IDataPreparation
{
    private readonly ISyntheticDataGenerator _syntheticDataGenerator;

    public DataPreparation(ISyntheticDataGenerator syntheticDataGenerator)
    {
        _syntheticDataGenerator = syntheticDataGenerator;
    }

    public GeneratedData PrepareGeneratedData(IGeneratorOptions generatorOptions)
    {
        DateTime endDate;
        DateTime startDate;
        Random randomNumberGenerator;
        List<DateTime> tradingDays;
        List<Instrument> generatedInstruments;
        List<PriceBar> generatedPriceBars;
        Dictionary<(int InstrumentId, DateTime PriceDate), decimal> closingPricesByInstrumentAndDate;
        List<Client> generatedClients;
        List<string> instrumentCurrencies;
        List<Account> generatedAccounts;
        List<Trade> generatedTrades;
        GeneratedData generatedDataToReturn;

        endDate = DateTime.UtcNow.Date.AddDays(-1);
        startDate = endDate.AddYears(-generatorOptions.Years);
        Console.WriteLine($"Generating synthetic data (seed {generatorOptions.Seed}, {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})...");

        randomNumberGenerator = new Random(generatorOptions.Seed);
        tradingDays = _syntheticDataGenerator.GetTradingDays(startDate, endDate);
        generatedInstruments = _syntheticDataGenerator.GenerateInstruments(
            randomNumberGenerator, generatorOptions.InstrumentCount);
        generatedPriceBars = _syntheticDataGenerator.GeneratePriceBars(
            randomNumberGenerator, generatedInstruments, tradingDays);
        closingPricesByInstrumentAndDate = generatedPriceBars.ToDictionary(
            priceBar =>
            {
                return (priceBar.InstrumentId, priceBar.PriceDate);
            },
            priceBar =>
            {
                return priceBar.Close;
            });
        generatedClients = _syntheticDataGenerator.GenerateClients(
            generatorOptions.Seed, generatorOptions.ClientCount, tradingDays[0]);
        instrumentCurrencies = generatedInstruments.Select(instrument =>
        {
            return instrument.Currency;
        }).Distinct().ToList();
        generatedAccounts = _syntheticDataGenerator.GenerateAccounts(
            randomNumberGenerator, generatedClients, tradingDays[0], tradingDays[^1], instrumentCurrencies);
        generatedTrades = _syntheticDataGenerator.GenerateTrades(
            randomNumberGenerator, generatedAccounts, generatedInstruments,
            closingPricesByInstrumentAndDate, tradingDays, generatorOptions.TradesPerAccount);

        generatedDataToReturn = new GeneratedData()
            .WithInstruments(generatedInstruments)
            .WithPrices(generatedPriceBars)
            .WithClients(generatedClients)
            .WithAccounts(generatedAccounts)
            .WithTrades(generatedTrades)
            .Build();
        return generatedDataToReturn;
    }
}
