using TradingDataGenerator.Models;

namespace TradingDataGenerator.Generation;

// Who sees which clients (row-level security, sql/02_security.sql). "local", the token from .env, sees every client.
// The rest is split into quarters: one each for copilot, claude and inspector, and one that all three share.
// Every Thompson is in the shared quarter, so the docs' example name works with any token.
// An Entra user or app is added by its object ID; see docs/04-azure-deployment.md.
internal static class CallerClientGenerator
{
    private static readonly string[] NamedTokens = ["copilot", "claude", "inspector"];

    public static List<CallerClient> Generate(IReadOnlyList<Client> clients) =>
        clients.SelectMany(client => CallersOf(client).Select(caller => new CallerClient(caller, client.Id))).ToList();

    private static IEnumerable<string> CallersOf(Client client)
    {
        yield return "local";

        var quarter = client.Id % 4;
        if (quarter == 0 || client.FullName.EndsWith(" Thompson", StringComparison.Ordinal))
        {
            foreach (var token in NamedTokens)
            {
                yield return token;
            }
        }
        else
        {
            yield return NamedTokens[quarter - 1];
        }
    }
}
