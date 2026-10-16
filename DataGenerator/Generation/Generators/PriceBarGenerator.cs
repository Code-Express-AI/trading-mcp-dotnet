using TradingDataGenerator.Models;
using TradingDataGenerator.Generation.Builders;

namespace TradingDataGenerator.Generation.Generators;

public sealed class PriceBarGenerator : IPriceBarGenerator
{
    public List<PriceBar> GeneratePriceBars(
        Random randomNumberGenerator,
        List<Instrument> generatedInstruments,
        List<DateTime> tradingDays)
    {
        const double tradingTimeStepInYears = 1.0 / 252.0;
        List<PriceBar> generatedPriceBarsToReturn;
        double previousClose;
        int baseVolume;
        double standardNormalSample;
        double closePrice;
        double openPrice;
        double highPrice;
        double lowPrice;
        long tradingVolume;

        generatedPriceBarsToReturn = new List<PriceBar>(generatedInstruments.Count * tradingDays.Count);
        foreach (var instrument in generatedInstruments)
        {
            previousClose = instrument.StartPrice;
            baseVolume = 200_000 + randomNumberGenerator.Next(0, 3_000_000);

            foreach (var tradingDay in tradingDays)
            {
                standardNormalSample = GenerateStandardNormalSample(randomNumberGenerator);
                closePrice = previousClose * Math.Exp(
                    (instrument.Mu - 0.5 * instrument.Sigma * instrument.Sigma) * tradingTimeStepInYears
                    + instrument.Sigma * Math.Sqrt(tradingTimeStepInYears) * standardNormalSample);
                closePrice = Math.Max(closePrice, 1.0);

                openPrice = previousClose * (1 + (randomNumberGenerator.NextDouble() - 0.5) * 0.01);
                highPrice = Math.Max(openPrice, closePrice) * (1 + randomNumberGenerator.NextDouble() * 0.01);
                lowPrice = Math.Min(openPrice, closePrice) * (1 - randomNumberGenerator.NextDouble() * 0.01);
                tradingVolume = (long)(baseVolume * Math.Exp(0.4 * GenerateStandardNormalSample(randomNumberGenerator)));
                generatedPriceBarsToReturn.Add(new PriceBarBuilder()
                    .WithInstrumentId(instrument.Id)
                    .WithPriceDate(tradingDay)
                    .WithOpen(RoundMoney(openPrice))
                    .WithHigh(RoundMoney(highPrice))
                    .WithLow(RoundMoney(Math.Max(lowPrice, 0.5)))
                    .WithClose(RoundMoney(closePrice))
                    .WithVolume(tradingVolume)
                    .Build());

                previousClose = closePrice;
            }
        }
        return generatedPriceBarsToReturn;
    }

    private decimal RoundMoney(double monetaryValue)
    {
        decimal roundedMonetaryValue;
        roundedMonetaryValue = (decimal)Math.Round(monetaryValue, 2);
        return roundedMonetaryValue;
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
