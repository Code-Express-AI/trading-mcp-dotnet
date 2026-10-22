using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation;

// Daily prices follow geometric Brownian motion, the standard textbook model for share prices.
// Each day the price changes by a random percentage: a small steady drift (the instrument's average growth)
// plus a random shock drawn from a normal distribution and scaled by the instrument's volatility.
// Working in percentages means prices can never go negative, and big moves are rare but possible.
internal static class PriceGenerator
{
    private const double OneTradingDayInYears = 1.0 / 252.0;

    public static List<PriceBar> Generate(Random random, IReadOnlyList<Instrument> instruments, IReadOnlyList<DateTime> tradingDays)
    {
        var prices = new List<PriceBar>(instruments.Count * tradingDays.Count);
        foreach (var instrument in instruments)
        {
            var previousClose = instrument.StartPrice;
            var typicalVolume = 200_000 + random.Next(0, 3_000_000);

            foreach (var tradingDay in tradingDays)
            {
                var close = NextClose(random, instrument, previousClose);
                prices.Add(CreatePriceBar(random, instrument.Id, tradingDay, previousClose, close, typicalVolume));
                previousClose = close;
            }
        }

        return prices;
    }

    private static double NextClose(Random random, Instrument instrument, double previousClose)
    {
        // The -½σ² term is the standard correction that keeps the average growth equal to the drift.
        var volatility = instrument.AnnualVolatility;
        var drift = (instrument.AnnualDrift - 0.5 * volatility * volatility) * OneTradingDayInYears;
        var shock = volatility * Math.Sqrt(OneTradingDayInYears) * random.NextStandardNormal();

        // Floor at 1.00 so a long losing streak can't leave a share worth fractions of a penny.
        return Math.Max(previousClose * Math.Exp(drift + shock), 1.0);
    }

    private static PriceBar CreatePriceBar(
        Random random, int instrumentId, DateTime tradingDay, double previousClose, double close, int typicalVolume)
    {
        // The open is within ±0.5% of yesterday's close. The high and low reach up to 1% beyond
        // the open and close, so the bar always satisfies Low <= Open, Close <= High.
        var open = previousClose * (1 + (random.NextDouble() - 0.5) * 0.01);
        var high = Math.Max(open, close) * (1 + random.NextDouble() * 0.01);
        var low = Math.Min(open, close) * (1 - random.NextDouble() * 0.01);

        // Volume varies log-normally around the instrument's typical daily volume: usually close to it,
        // occasionally two or three times higher.
        var volume = (long)(typicalVolume * Math.Exp(0.4 * random.NextStandardNormal()));

        return new PriceBar(
            InstrumentId: instrumentId,
            PriceDate: tradingDay,
            Open: RoundMoney(open),
            High: RoundMoney(high),
            Low: RoundMoney(Math.Max(low, 0.5)),
            Close: RoundMoney(close),
            Volume: volume);
    }

    private static decimal RoundMoney(double value) => (decimal)Math.Round(value, 2);
}
