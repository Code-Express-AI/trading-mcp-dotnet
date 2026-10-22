using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation;

internal static class InstrumentGenerator
{
    // Company names are a random prefix plus a sector suffix, e.g. "Marlowe Pharma".
    public static List<Instrument> Generate(Random random, int count)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, SeedData.MaxInstruments);

        var suffixes = SeedData.CompanyNameSuffixes.Keys.ToList();
        var usedNames = new HashSet<string>();
        var usedTickers = new HashSet<string>();
        var instruments = new List<Instrument>(count);

        while (instruments.Count < count)
        {
            var prefix = random.Pick(SeedData.CompanyNamePrefixes);
            var suffix = random.Pick(suffixes);
            var name = $"{prefix} {suffix}";
            if (!usedNames.Add(name))
            {
                continue;
            }

            var (sector, annualDrift, annualVolatility) = SeedData.CompanyNameSuffixes[suffix];
            instruments.Add(new Instrument(
                Id: instruments.Count + 1,
                Ticker: CreateUniqueTicker(random, prefix, suffix, usedTickers),
                Name: name,
                Sector: sector,
                Currency: random.Pick(SeedData.Currencies),
                StartPrice: CreateStartPrice(random),
                // Each company varies a little from its sector: drift by up to ±2 percentage points,
                // volatility by up to ±15%.
                AnnualDrift: annualDrift + (random.NextDouble() - 0.5) * 0.04,
                AnnualVolatility: annualVolatility * (0.85 + random.NextDouble() * 0.3)));
        }

        return instruments;
    }

    // Prefer readable tickers: "Marlowe Pharma" becomes MARP, then MAPH.
    // If both are taken, fall back to the first three letters plus a random letter.
    private static string CreateUniqueTicker(Random random, string prefix, string suffix, HashSet<string> usedTickers)
    {
        string[] candidates = [(prefix[..3] + suffix[..1]).ToUpperInvariant(), (prefix[..2] + suffix[..2]).ToUpperInvariant()];
        foreach (var candidate in candidates)
        {
            if (usedTickers.Add(candidate))
            {
                return candidate;
            }
        }

        string ticker;
        do
        {
            ticker = (prefix[..3] + (char)('A' + random.Next(26))).ToUpperInvariant();
        }
        while (!usedTickers.Add(ticker));

        return ticker;
    }

    // Log-normal, like real share prices: most start between about 10 and 150, centred near 45,
    // with a few expensive outliers. Clamped to 5-600.
    private static double CreateStartPrice(Random random) =>
        Math.Clamp(Math.Round(10 * Math.Exp(1.5 + 0.8 * random.NextStandardNormal()), 2), 5, 600);
}
