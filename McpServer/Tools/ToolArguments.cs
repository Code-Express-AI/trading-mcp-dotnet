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

    public static void RequireDateRange(DateOnly? from, DateOnly? to) =>
        Require(from is null || to is null || from <= to, "from must be on or before to.");
}
