using System.Globalization;

namespace TradingMcp.Tools;

// Guard clauses for tool arguments. Rejecting a bad argument with a clear message matters more for a model
// than for a person: an empty result looks like "there's nothing there", and the conversation carries on
// believing it.
internal static class ToolArguments
{
    public static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new ToolArgumentException(message);
        }
    }

    // A string, not a Guid parameter, for the same reason as the dates below: a malformed key gets our message.
    public static Guid ParseAccountKey(string? accountKey)
    {
        Require(Guid.TryParse(accountKey?.Trim(), out var key),
            "accountKey must be the accountKey returned by find_accounts, for example 3f2b8c1e-7d4a-4e9b-9c61-2a5d8e0f4b17.");
        return key;
    }

    // Dates arrive as plain strings, empty when left out. A DateOnly? parameter would make the schema type
    // ["string", "null"], which some clients reject, and a badly formatted date would fail inside the SDK
    // with a generic error instead of a message the model can act on.
    // SQL Server can't handle every DateOnly (for example 9999-12-31 plus a day), so keep dates to a sensible range.
    public static DateOnly? ParseDate(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        Require(DateOnly.TryParseExact(value.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date),
            $"{name} must be a date in yyyy-MM-dd format, for example 2026-06-30.");
        Require(date >= EarliestDate && date <= LatestDate,
            "Dates must be between 1900-01-01 and 2100-12-31. Leave the date out for no limit, or call get_dataset_info for the dates available.");
        return date;
    }

    public static (DateOnly? From, DateOnly? To) ParseDateRange(string? from, string? to)
    {
        var fromDate = ParseDate(from, nameof(from));
        var toDate = ParseDate(to, nameof(to));
        Require(fromDate is null || toDate is null || fromDate <= toDate, "from must be on or before to.");
        return (fromDate, toDate);
    }

    private static readonly DateOnly EarliestDate = new(1900, 1, 1);
    private static readonly DateOnly LatestDate = new(2100, 12, 31);
}
