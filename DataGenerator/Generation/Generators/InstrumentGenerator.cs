using TradingDataGenerator.Models;
using TradingDataGenerator.Generation.Builders;

namespace TradingDataGenerator.Generation.Generators;

public sealed class InstrumentGenerator : IInstrumentGenerator
{
    public List<Instrument> GenerateInstruments(Random randomNumberGenerator, int instrumentCount)
    {
        string[] suffixKeys;
        string[] availableCurrencies;
        HashSet<string> usedInstrumentNames;
        HashSet<string> usedTickers;
        List<Instrument> generatedInstrumentsToReturn;
        string companyPrefix;
        string companySuffix;
        string instrumentName;
        string instrumentSector;
        double annualDrift;
        double annualVolatility;

        instrumentCount = Math.Min(instrumentCount, SeedData.CompanyNamePrefixes.Count * SeedData.CompanyNameSuffixes.Count);
        suffixKeys = SeedData.CompanyNameSuffixes.Keys.ToArray();
        availableCurrencies = SeedData.Currencies.ToArray();
        usedInstrumentNames = new HashSet<string>();
        usedTickers = new HashSet<string>();
        generatedInstrumentsToReturn = new List<Instrument>(instrumentCount);

        while (generatedInstrumentsToReturn.Count < instrumentCount)
        {
            companyPrefix = SeedData.CompanyNamePrefixes[randomNumberGenerator.Next(SeedData.CompanyNamePrefixes.Count)];
            companySuffix = suffixKeys[randomNumberGenerator.Next(suffixKeys.Length)];
            instrumentName = $"{companyPrefix} {companySuffix}";
            if (!usedInstrumentNames.Add(instrumentName))
            {
                continue;
            }

            (instrumentSector, annualDrift, annualVolatility) = SeedData.CompanyNameSuffixes[companySuffix];
            generatedInstrumentsToReturn.Add(new InstrumentBuilder()
                .WithId(generatedInstrumentsToReturn.Count + 1)
                .WithTicker(CreateTicker(randomNumberGenerator, companyPrefix, companySuffix, usedTickers))
                .WithName(instrumentName)
                .WithSector(instrumentSector)
                .WithCurrency(availableCurrencies[randomNumberGenerator.Next(availableCurrencies.Length)])
                .WithStartPrice(Math.Clamp(
                    Math.Round(10 * Math.Exp(1.5 + 0.8 * GenerateStandardNormalSample(randomNumberGenerator)), 2),
                    5, 600))
                .WithMu(annualDrift + (randomNumberGenerator.NextDouble() - 0.5) * 0.04)
                .WithSigma(annualVolatility * (0.85 + randomNumberGenerator.NextDouble() * 0.3))
                .Build());
        }
        return generatedInstrumentsToReturn;
    }

    private string CreateTicker(
        Random randomNumberGenerator,
        string companyPrefix,
        string companySuffix,
        HashSet<string> usedTickers)
    {
        string[] candidateTickers;
        string generatedTicker;

        candidateTickers = new[]
        {
            (companyPrefix[..3] + companySuffix[..1]).ToUpperInvariant(),
            (companyPrefix[..2] + companySuffix[..2]).ToUpperInvariant(),
        };
        foreach (var candidateTicker in candidateTickers)
        {
            if (usedTickers.Add(candidateTicker))
            {
                return candidateTicker;
            }
        }

        do
        {
            generatedTicker = (companyPrefix[..3] + (char)('A' + randomNumberGenerator.Next(26))).ToUpperInvariant();
        }
        while (!usedTickers.Add(generatedTicker));

        return generatedTicker;
    }

    private double GenerateStandardNormalSample(Random randomNumberGenerator)
    {
        double firstUniformSample;
        double secondUniformSample;
        double standardNormalSample;

        firstUniformSample = 1.0 - randomNumberGenerator.NextDouble();
        secondUniformSample = randomNumberGenerator.NextDouble();
        standardNormalSample = Math.Sqrt(-2.0 * Math.Log(firstUniformSample))
                              * Math.Cos(2.0 * Math.PI * secondUniformSample);
        return standardNormalSample;
    }
}
