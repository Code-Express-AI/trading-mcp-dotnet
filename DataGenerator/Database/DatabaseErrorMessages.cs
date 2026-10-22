using System.Net.Sockets;
using System.Security.Authentication;
using Microsoft.Data.SqlClient;

namespace TradingDataGenerator.Database;

// Turns the SQL Server errors people hit most often when setting up the demo into plain advice.
// Windows reports connection problems as SQL error numbers. macOS and Linux report them as error number 0,
// sometimes with a SocketException attached.
internal static class DatabaseErrorMessages
{
    private const int ClientSideConnectionError = 0;
    private const int HostNotFound = 11001;
    private const int LoginFailed = 18456;
    private const int CannotOpenDatabase = 4060;
    private const int InvalidObjectName = 208;
    private const int CertificateNotTrusted = -2146893019;

    private static readonly int[] CannotConnect = [-2, 2, 53, 258, 10060, 10061];
    private static readonly int[] PermissionDenied = [229, 230, 262, 4834];

    private const string PortConflictHint =
        "- If SQL Server is also installed on this machine (common on Windows), it may be using port 1433 " +
        "and answering instead of the container. Stop that SQL Server service, then run: docker compose up -d";

    public static string Describe(SqlException exception, string connectionString) =>
        Describe(exception.Number, exception.Message, CausesOf(exception), connectionString);

    internal static string Describe(int errorNumber, string errorMessage, IReadOnlyList<Exception> causes, string connectionString)
    {
        var connection = new SqlConnectionStringBuilder(connectionString);
        var socketError = causes.OfType<SocketException>().FirstOrDefault()?.SocketErrorCode;

        if (socketError is SocketError.HostNotFound or SocketError.NoData || errorNumber == HostNotFound)
        {
            return ServerNotFound(connection.DataSource);
        }

        if (causes.OfType<AuthenticationException>().Any() || errorNumber == CertificateNotTrusted
            || errorMessage.Contains("certificate", StringComparison.OrdinalIgnoreCase))
        {
            return UntrustedCertificate();
        }

        if (socketError is not null || errorNumber == ClientSideConnectionError || CannotConnect.Contains(errorNumber))
        {
            return CouldNotConnect(connection.DataSource);
        }

        if (PermissionDenied.Contains(errorNumber))
        {
            return NoPermission(connection.UserID);
        }

        return errorNumber switch
        {
            LoginFailed => LoginRejected(connection.DataSource, connection.UserID),
            CannotOpenDatabase => DatabaseMissing(connection.DataSource, connection.InitialCatalog),
            InvalidObjectName => TablesMissing(connection.InitialCatalog),
            _ => $"SQL Server error {errorNumber}: {errorMessage}"
        };
    }

    private static List<Exception> CausesOf(Exception exception)
    {
        var causes = new List<Exception>();
        for (var cause = exception.InnerException; cause is not null; cause = cause.InnerException)
        {
            causes.Add(cause);
        }

        return causes;
    }

    private static string ServerNotFound(string server) => Lines(
        $"Couldn't find a server called '{server}'.",
        "- Check the server name in the connection string. For the local container use: Server=localhost,1433");

    private static string CouldNotConnect(string server) => Lines(
        $"Couldn't connect to SQL Server at {server}.",
        "- Check Docker Desktop is running and the container is up: docker compose ps",
        "- If it isn't, start it: docker compose up -d",
        "- If Docker says port 1433 is already allocated, another SQL Server is probably installed on this machine. " +
        "Stop that SQL Server service, then run: docker compose up -d",
        "- Check the server and port in the connection string. For the local container use: Server=localhost,1433");

    private static string UntrustedCertificate() => Lines(
        "SQL Server's certificate isn't trusted.",
        "- For the local Docker container, add TrustServerCertificate=True to the connection string.",
        "- Don't use TrustServerCertificate=True for Azure SQL.");

    private static string LoginRejected(string server, string user) => Lines(
        $"SQL Server at {server} rejected the login for user '{user}'.",
        "- Check the password matches SA_PASSWORD in your .env file.",
        "- SA_PASSWORD only applies when the container is first created. If you changed it afterwards, " +
        "delete the database and start again: docker compose down -v",
        PortConflictHint);

    private static string DatabaseMissing(string server, string database) => Lines(
        $"The database '{database}' doesn't exist on {server}.",
        "- Create it: docker compose run --rm db-init",
        PortConflictHint);

    private static string TablesMissing(string database) => Lines(
        $"The trading tables don't exist in '{database}'.",
        "- Create them: docker compose run --rm db-init",
        "- If db-init says the database already exists, recreate it: docker compose run --rm -e RECREATE=1 db-init");

    private static string NoPermission(string user) => Lines(
        $"User '{user}' doesn't have permission to load data.",
        "- Connect as sa (or another admin), not as mcp_reader.");

    private static string Lines(params string[] lines) => string.Join(Environment.NewLine, lines);
}
