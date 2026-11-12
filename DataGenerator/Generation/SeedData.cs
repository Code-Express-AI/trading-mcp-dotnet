using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation;

internal static class SeedData
{
    public static IReadOnlyList<string> Countries { get; } =
        ["United Kingdom", "United States", "Germany", "France", "Ireland", "Netherlands", "Spain", "Italy"];

    public static IReadOnlyList<string> RiskProfiles { get; } = ["Conservative", "Balanced", "Growth", "Aggressive"];

    // Repeated values are weights: half of all accounts are brokerage accounts.
    public static IReadOnlyList<string> AccountTypes { get; } = ["Brokerage", "Brokerage", "Retirement", "Joint"];

    // Weighted 60% USD, 20% GBP, 20% EUR.
    public static IReadOnlyList<string> Currencies { get; } =
        ["USD", "USD", "USD", "USD", "USD", "USD", "GBP", "GBP", "EUR", "EUR"];

    public static IReadOnlyList<string> CompanyNamePrefixes { get; } =
    [
        "Aldergate", "Brightwater", "Cinderfall", "Dunmore", "Eastvale", "Falconer", "Glenhaven",
        "Harrowgate", "Ironbridge", "Juniper", "Kestrel", "Lakemont", "Marlowe", "Northgate",
        "Oakhurst", "Pinecrest", "Quillfield", "Redstone", "Silverpine", "Thornbury", "Umberfield",
        "Valewood", "Westmere", "Yarrow", "Zephyrine"
    ];

    // The suffix sets the sector and its typical behaviour: annual drift (average growth)
    // and annual volatility (how much the price swings). Technology grows fastest and swings most;
    // utilities are slow and steady.
    public static IReadOnlyDictionary<string, (string Sector, double AnnualDrift, double AnnualVolatility)> CompanyNameSuffixes { get; } =
        new Dictionary<string, (string, double, double)>
        {
            ["Holdings"] = ("Financials", 0.07, 0.22),
            ["Bank"] = ("Financials", 0.06, 0.20),
            ["Capital"] = ("Financials", 0.08, 0.25),
            ["Insurance"] = ("Financials", 0.05, 0.16),
            ["Energy"] = ("Energy", 0.05, 0.32),
            ["Utilities"] = ("Utilities", 0.04, 0.14),
            ["Pharma"] = ("Healthcare", 0.09, 0.28),
            ["Systems"] = ("Technology", 0.12, 0.35),
            ["Networks"] = ("Technology", 0.11, 0.33),
            ["Foods"] = ("Consumer Staples", 0.05, 0.15),
            ["Retail"] = ("Consumer Discretionary", 0.06, 0.24),
            ["Motors"] = ("Consumer Discretionary", 0.06, 0.30),
            ["Logistics"] = ("Industrials", 0.07, 0.21),
            ["Materials"] = ("Materials", 0.06, 0.26),
            ["Media"] = ("Communication", 0.07, 0.27)
        };

    public static int MaxInstruments => CompanyNamePrefixes.Count * CompanyNameSuffixes.Count;

    // The docs and videos search for "Thompson", so these two clients always exist, whatever the seed or Bogus version.
    // Every Thompson gets at least two accounts (AccountGenerator) and is visible to every token (CallerClientGenerator),
    // so a search for "Thompson" always finds at least four accounts. With seed 42, Bogus picks these names anyway.
    public const string ExampleLastName = "Thompson";
    public const int ExampleClientMinimumAccounts = 2;

    public static IReadOnlyDictionary<int, string> ExampleClientFirstNames { get; } = new Dictionary<int, string>
    {
        [34] = "Giovanny",
        [53] = "Tessie"
    };

    public static bool IsExampleClient(Client client) => client.FullName.EndsWith($" {ExampleLastName}", StringComparison.Ordinal);
}
