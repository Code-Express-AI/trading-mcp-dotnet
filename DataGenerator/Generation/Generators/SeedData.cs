using System.Collections.ObjectModel;

namespace TradingDataGenerator.Generation.Generators;

public static class SeedData
{
    private static readonly string[] _countries =
    {
        "United Kingdom", "United States", "Germany", "France", "Ireland", "Netherlands", "Spain", "Italy"
    };

    private static readonly string[] _riskProfiles =
    {
        "Conservative", "Balanced", "Growth", "Aggressive"
    };

    private static readonly string[] _accountTypes =
    {
        "Brokerage", "Brokerage", "Retirement", "Joint"
    };

    private static readonly string[] _currencies =
    {
        "USD", "USD", "USD", "USD", "USD", "USD", "GBP", "GBP", "EUR", "EUR"
    };

    private static readonly string[] _companyNamePrefixes =
    {
        "Aldergate", "Brightwater", "Cinderfall", "Dunmore", "Eastvale", "Falconer", "Glenhaven",
        "Harrowgate", "Ironbridge", "Juniper", "Kestrel", "Lakemont", "Marlowe", "Northgate",
        "Oakhurst", "Pinecrest", "Quillfield", "Redstone", "Silverpine", "Thornbury", "Umberfield",
        "Valewood", "Westmere", "Yarrow", "Zephyrine"
    };

    private static readonly IReadOnlyDictionary<string, (string Sector, double Mu, double Sigma)> _companyNameSuffixes =
        new ReadOnlyDictionary<string, (string Sector, double Mu, double Sigma)>(
            new Dictionary<string, (string Sector, double Mu, double Sigma)>
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
                ["Media"] = ("Communication", 0.07, 0.27),
            });

    public static IReadOnlyList<string> Countries => _countries;
    public static IReadOnlyList<string> RiskProfiles => _riskProfiles;
    public static IReadOnlyList<string> AccountTypes => _accountTypes;
    public static IReadOnlyList<string> Currencies => _currencies;
    public static IReadOnlyList<string> CompanyNamePrefixes => _companyNamePrefixes;
    public static IReadOnlyDictionary<string, (string Sector, double Mu, double Sigma)> CompanyNameSuffixes =>
        _companyNameSuffixes;
}
