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

    public static void RequireAccountId(int accountId) =>
        Require(accountId >= 1, "accountId must be a positive number. Use find_accounts to look it up.");

    // SQL Server can't handle every DateOnly (for example 9999-12-31 plus a day), so keep dates to a sensible range.
    public static void RequireDate(DateOnly? date) =>
        Require(date is null || (date >= EarliestDate && date <= LatestDate),
            "Dates must be between 1900-01-01 and 2100-12-31. Leave the date out for no limit, or call get_dataset_info for the dates available.");

    public static void RequireDateRange(DateOnly? from, DateOnly? to)
    {
        RequireDate(from);
        RequireDate(to);
        Require(from is null || to is null || from <= to, "from must be on or before to.");
    }

    private static readonly DateOnly EarliestDate = new(1900, 1, 1);
    private static readonly DateOnly LatestDate = new(2100, 12, 31);
}
