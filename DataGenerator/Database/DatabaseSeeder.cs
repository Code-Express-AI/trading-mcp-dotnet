using System.Data;
using Microsoft.Data.SqlClient;
using TradingDataGenerator.Models;

namespace TradingDataGenerator.Database;

// Loads everything in a single transaction: either all the data goes in, or none of it does.
internal sealed class DatabaseSeeder(string connectionString) : IDatabaseSeeder
{
    public async Task<bool> ContainsDataAsync()
    {
        await using var connection = await OpenConnectionAsync();
        return await ExecuteScalarIntAsync(connection, null, "SELECT COUNT(*) FROM trading.Instruments") > 0;
    }

    public async Task SeedAsync(GeneratedData data, bool clearExistingData)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        if (clearExistingData)
        {
            await ClearDataAsync(connection, transaction);
        }

        await InsertDataAsync(connection, transaction, data);
        var openPositionCount = await ExecuteScalarIntAsync(connection, transaction, "SELECT COUNT(*) FROM trading.v_Positions");
        await transaction.CommitAsync();

        Console.WriteLine($"Done. {openPositionCount:N0} open positions across {data.Accounts.Count:N0} accounts.");
    }

    private async Task<SqlConnection> OpenConnectionAsync()
    {
        var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync();
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static async Task ClearDataAsync(SqlConnection connection, SqlTransaction transaction)
    {
        Console.WriteLine("Clearing existing data (AuditLog is left alone)...");

        // Children before parents, so no foreign key is ever left pointing at a deleted row.
        await ExecuteNonQueryAsync(connection, transaction, "DELETE FROM trading.Trades");
        await ExecuteNonQueryAsync(connection, transaction, "DELETE FROM trading.Prices");
        await ExecuteNonQueryAsync(connection, transaction, "DELETE FROM trading.Accounts");
        await ExecuteNonQueryAsync(connection, transaction, "DELETE FROM trading.Clients");
        await ExecuteNonQueryAsync(connection, transaction, "DELETE FROM trading.Instruments");

        // Trades are the only table where SQL Server assigns the IDs, so restart them at 1.
        await ExecuteNonQueryAsync(connection, transaction, "DBCC CHECKIDENT ('trading.Trades', RESEED, 0)");
    }

    private static async Task InsertDataAsync(SqlConnection connection, SqlTransaction transaction, GeneratedData data)
    {
        Console.WriteLine("Inserting...");

        // Parents before children. The generated IDs are kept (keepIdentity) so the foreign keys line up.
        await BulkInsertAsync(connection, transaction, "trading.Instruments", CreateInstrumentTable(data.Instruments), keepIdentity: true);
        await BulkInsertAsync(connection, transaction, "trading.Prices", CreatePriceTable(data.Prices), keepIdentity: false);
        await BulkInsertAsync(connection, transaction, "trading.Clients", CreateClientTable(data.Clients), keepIdentity: true);
        await BulkInsertAsync(connection, transaction, "trading.Accounts", CreateAccountTable(data.Accounts), keepIdentity: true);
        await BulkInsertAsync(connection, transaction, "trading.Trades", CreateTradeTable(data.Trades), keepIdentity: false);
    }

    private static DataTable CreateInstrumentTable(IEnumerable<Instrument> instruments)
    {
        var table = CreateTable(
            ("InstrumentId", typeof(int)), ("Ticker", typeof(string)), ("Name", typeof(string)),
            ("Sector", typeof(string)), ("Currency", typeof(string)));
        foreach (var instrument in instruments)
        {
            table.Rows.Add(instrument.Id, instrument.Ticker, instrument.Name, instrument.Sector, instrument.Currency);
        }

        return table;
    }

    private static DataTable CreatePriceTable(IEnumerable<PriceBar> prices)
    {
        var table = CreateTable(
            ("InstrumentId", typeof(int)), ("PriceDate", typeof(DateTime)), ("OpenPrice", typeof(decimal)),
            ("HighPrice", typeof(decimal)), ("LowPrice", typeof(decimal)), ("ClosePrice", typeof(decimal)), ("Volume", typeof(long)));
        foreach (var price in prices)
        {
            table.Rows.Add(price.InstrumentId, price.PriceDate, price.Open, price.High, price.Low, price.Close, price.Volume);
        }

        return table;
    }

    private static DataTable CreateClientTable(IEnumerable<Client> clients)
    {
        var table = CreateTable(
            ("ClientId", typeof(int)), ("FullName", typeof(string)), ("Email", typeof(string)),
            ("Country", typeof(string)), ("RiskProfile", typeof(string)), ("CreatedOn", typeof(DateTime)));
        foreach (var client in clients)
        {
            table.Rows.Add(client.Id, client.FullName, client.Email, client.Country, client.RiskProfile, client.CreatedOn);
        }

        return table;
    }

    private static DataTable CreateAccountTable(IEnumerable<Account> accounts)
    {
        var table = CreateTable(
            ("AccountId", typeof(int)), ("ClientId", typeof(int)), ("AccountNumber", typeof(string)),
            ("AccountType", typeof(string)), ("BaseCurrency", typeof(string)), ("OpenedOn", typeof(DateTime)));
        foreach (var account in accounts)
        {
            table.Rows.Add(account.Id, account.ClientId, account.AccountNumber, account.AccountType, account.BaseCurrency, account.OpenedOn);
        }

        return table;
    }

    private static DataTable CreateTradeTable(IEnumerable<Trade> trades)
    {
        var table = CreateTable(
            ("AccountId", typeof(int)), ("InstrumentId", typeof(int)), ("Side", typeof(string)), ("Quantity", typeof(decimal)),
            ("Price", typeof(decimal)), ("Fees", typeof(decimal)), ("TradeTimestamp", typeof(DateTime)), ("Status", typeof(string)));
        foreach (var trade in trades)
        {
            table.Rows.Add(trade.AccountId, trade.InstrumentId, trade.Side, trade.Quantity,
                           trade.Price, trade.Fees, trade.Timestamp, trade.Status);
        }

        return table;
    }

    private static DataTable CreateTable(params (string Name, Type Type)[] columns)
    {
        var table = new DataTable();
        foreach (var (name, type) in columns)
        {
            table.Columns.Add(name, type);
        }

        return table;
    }

    private static async Task BulkInsertAsync(
        SqlConnection connection, SqlTransaction transaction, string tableName, DataTable rows, bool keepIdentity)
    {
        var options = SqlBulkCopyOptions.TableLock | (keepIdentity ? SqlBulkCopyOptions.KeepIdentity : SqlBulkCopyOptions.Default);
        using var bulkCopy = new SqlBulkCopy(connection, options, transaction)
        {
            DestinationTableName = tableName,
            BatchSize = 5000,
            BulkCopyTimeout = 300
        };

        // Map by name, so the column order in the DataTable doesn't have to match the table.
        foreach (DataColumn column in rows.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(rows);
        Console.WriteLine($"  {tableName,-22} {rows.Rows.Count,9:N0} rows");
    }

    private static async Task<int> ExecuteScalarIntAsync(SqlConnection connection, SqlTransaction? transaction, string sql)
    {
        await using var command = new SqlCommand(sql, connection, transaction);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task ExecuteNonQueryAsync(SqlConnection connection, SqlTransaction transaction, string sql)
    {
        await using var command = new SqlCommand(sql, connection, transaction) { CommandTimeout = 120 };
        await command.ExecuteNonQueryAsync();
    }
}
