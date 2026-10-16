using System.Data;
using Microsoft.Data.SqlClient;
using TradingDataGenerator.Constants;
using TradingDataGenerator.Models;

namespace TradingDataGenerator.Database;

public sealed class DatabaseSeeder : IDatabaseSeeder
{
    private readonly string _databaseConnectionString;

    public DatabaseSeeder(string databaseConnectionString)
    {
        _databaseConnectionString = databaseConnectionString;
    }

    public async Task<int> SeedAsync(GeneratedData generatedData)
    {
        int existingInstrumentCount;
        SqlTransaction sqlTransaction;
        await using var sqlConnection = new SqlConnection(_databaseConnectionString);
        await sqlConnection.OpenAsync();

        await using (sqlTransaction = (SqlTransaction)await sqlConnection.BeginTransactionAsync())
        {
            existingInstrumentCount = await ExecuteScalarIntAsync(
                sqlConnection, sqlTransaction, "SELECT COUNT(*) FROM trading.Instruments");
            if (existingInstrumentCount > 0)
            {
                Console.Error.WriteLine(ErrorMessages.DatabaseAlreadyContainsData);
                return ExitCodes.DatabaseAlreadyContainsData;
            }

            return await InsertAndCommitAsync(sqlConnection, sqlTransaction, generatedData);
        }
    }

    public async Task<int> SeedAsyncWithReset(GeneratedData generatedData)
    {
        int existingInstrumentCount;
        SqlTransaction sqlTransaction;
        await using var sqlConnection = new SqlConnection(_databaseConnectionString);
        await sqlConnection.OpenAsync();

        await using (sqlTransaction = (SqlTransaction)await sqlConnection.BeginTransactionAsync())
        {
            existingInstrumentCount = await ExecuteScalarIntAsync(
                sqlConnection, sqlTransaction, "SELECT COUNT(*) FROM trading.Instruments");
            if (existingInstrumentCount > 0)
            {
                await ClearDataAsync(sqlConnection, sqlTransaction);
            }

            return await InsertAndCommitAsync(sqlConnection, sqlTransaction, generatedData);
        }
    }

    private async Task<int> InsertAndCommitAsync(
        SqlConnection sqlConnection, SqlTransaction sqlTransaction, GeneratedData generatedData)
    {
        int openPositionCount;
        await InsertDataAsync(sqlConnection, sqlTransaction, generatedData);
        openPositionCount = await ExecuteScalarIntAsync(
            sqlConnection, sqlTransaction, "SELECT COUNT(*) FROM trading.v_Positions");
        await sqlTransaction.CommitAsync();

        Console.WriteLine($"Done. {openPositionCount:N0} open positions across {generatedData.Accounts.Count:N0} accounts.");
        return ExitCodes.Success;
    }

    private async Task ClearDataAsync(SqlConnection sqlConnection, SqlTransaction sqlTransaction)
    {
        Console.WriteLine("Clearing existing data (AuditLog is left alone)...");
        await ExecuteNonQueryAsync(sqlConnection, sqlTransaction, "DELETE FROM trading.Trades");
        await ExecuteNonQueryAsync(sqlConnection, sqlTransaction, "DELETE FROM trading.Prices");
        await ExecuteNonQueryAsync(sqlConnection, sqlTransaction, "DELETE FROM trading.Accounts");
        await ExecuteNonQueryAsync(sqlConnection, sqlTransaction, "DELETE FROM trading.Clients");
        await ExecuteNonQueryAsync(sqlConnection, sqlTransaction, "DELETE FROM trading.Instruments");
        await ExecuteNonQueryAsync(sqlConnection, sqlTransaction, "DBCC CHECKIDENT ('trading.Trades', RESEED, 0)");
    }

    private async Task InsertDataAsync(
        SqlConnection sqlConnection, SqlTransaction sqlTransaction, GeneratedData generatedData)
    {
        Console.WriteLine("Inserting...");

        await BulkAsync(sqlConnection, sqlTransaction, "trading.Instruments",
            CreateInstrumentTable(generatedData.Instruments), preserveIdentityValues: true);
        await BulkAsync(sqlConnection, sqlTransaction, "trading.Prices",
            CreatePriceTable(generatedData.Prices), preserveIdentityValues: false);
        await BulkAsync(sqlConnection, sqlTransaction, "trading.Clients",
            CreateClientTable(generatedData.Clients), preserveIdentityValues: true);
        await BulkAsync(sqlConnection, sqlTransaction, "trading.Accounts",
            CreateAccountTable(generatedData.Accounts), preserveIdentityValues: true);
        await BulkAsync(sqlConnection, sqlTransaction, "trading.Trades",
            CreateTradeTable(generatedData.Trades), preserveIdentityValues: false);
    }

    private DataTable CreateInstrumentTable(IEnumerable<Instrument> generatedInstruments)
    {
        DataTable tableToReturn;
        tableToReturn = CreateTable(("InstrumentId", typeof(int)), ("Ticker", typeof(string)), ("Name", typeof(string)),
                                    ("Sector", typeof(string)), ("Currency", typeof(string)));
        foreach (var generatedInstrument in generatedInstruments)
        {
            tableToReturn.Rows.Add(generatedInstrument.Id, generatedInstrument.Ticker, generatedInstrument.Name,
                                   generatedInstrument.Sector, generatedInstrument.Currency);
        }
        return tableToReturn;
    }

    private DataTable CreatePriceTable(IEnumerable<PriceBar> generatedPriceBars)
    {
        DataTable tableToReturn;
        tableToReturn = CreateTable(("InstrumentId", typeof(int)), ("PriceDate", typeof(DateTime)),
                                    ("OpenPrice", typeof(decimal)), ("HighPrice", typeof(decimal)),
                                    ("LowPrice", typeof(decimal)), ("ClosePrice", typeof(decimal)), ("Volume", typeof(long)));
        foreach (var priceBar in generatedPriceBars)
        {
            tableToReturn.Rows.Add(priceBar.InstrumentId, priceBar.PriceDate, priceBar.Open, priceBar.High,
                                   priceBar.Low, priceBar.Close, priceBar.Volume);
        }
        return tableToReturn;
    }

    private DataTable CreateClientTable(IEnumerable<Client> generatedClients)
    {
        DataTable tableToReturn;
        tableToReturn = CreateTable(("ClientId", typeof(int)), ("FullName", typeof(string)), ("Email", typeof(string)),
                                    ("Country", typeof(string)), ("RiskProfile", typeof(string)), ("CreatedOn", typeof(DateTime)));
        foreach (var generatedClient in generatedClients)
        {
            tableToReturn.Rows.Add(generatedClient.Id, generatedClient.FullName, generatedClient.Email,
                                   generatedClient.Country, generatedClient.RiskProfile, generatedClient.CreatedOn);
        }
        return tableToReturn;
    }

    private DataTable CreateAccountTable(IEnumerable<Account> generatedAccounts)
    {
        DataTable tableToReturn;
        tableToReturn = CreateTable(("AccountId", typeof(int)), ("ClientId", typeof(int)), ("AccountNumber", typeof(string)),
                                    ("AccountType", typeof(string)), ("BaseCurrency", typeof(string)), ("OpenedOn", typeof(DateTime)));
        foreach (var generatedAccount in generatedAccounts)
        {
            tableToReturn.Rows.Add(generatedAccount.Id, generatedAccount.ClientId, generatedAccount.AccountNumber,
                                   generatedAccount.AccountType, generatedAccount.BaseCurrency, generatedAccount.OpenedOn);
        }
        return tableToReturn;
    }

    private DataTable CreateTradeTable(IEnumerable<Trade> generatedTrades)
    {
        DataTable tableToReturn;
        tableToReturn = CreateTable(("AccountId", typeof(int)), ("InstrumentId", typeof(int)), ("Side", typeof(string)),
                                    ("Quantity", typeof(decimal)), ("Price", typeof(decimal)), ("Fees", typeof(decimal)),
                                    ("TradeTimestamp", typeof(DateTime)), ("Status", typeof(string)));
        foreach (var generatedTrade in generatedTrades)
        {
            tableToReturn.Rows.Add(generatedTrade.AccountId, generatedTrade.InstrumentId, generatedTrade.Side,
                                   generatedTrade.Quantity, generatedTrade.Price, generatedTrade.Fees,
                                   generatedTrade.Timestamp, generatedTrade.Status);
        }
        return tableToReturn;
    }

    private DataTable CreateTable(params (string Name, Type Type)[] tableColumns)
    {
        DataTable tableToReturn;
        tableToReturn = new DataTable();
        foreach (var (columnName, columnType) in tableColumns)
        {
            tableToReturn.Columns.Add(columnName, columnType);
        }
        return tableToReturn;
    }

    private async Task BulkAsync(
        SqlConnection sqlConnection,
        SqlTransaction sqlTransaction,
        string destinationTableName,
        DataTable tableData,
        bool preserveIdentityValues)
    {
        SqlBulkCopyOptions bulkCopyOptions;
        SqlBulkCopy bulkCopy;
        bulkCopyOptions = SqlBulkCopyOptions.TableLock
                          | (preserveIdentityValues
                              ? SqlBulkCopyOptions.KeepIdentity
                              : SqlBulkCopyOptions.Default);
        using (bulkCopy = new SqlBulkCopy(sqlConnection, bulkCopyOptions, sqlTransaction)
        {
            DestinationTableName = destinationTableName,
            BatchSize = 5000,
            BulkCopyTimeout = 300
        })
        {
            foreach (DataColumn tableColumn in tableData.Columns)
            {
                bulkCopy.ColumnMappings.Add(tableColumn.ColumnName, tableColumn.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(tableData);
            Console.WriteLine($"  {destinationTableName,-22} {tableData.Rows.Count,9:N0} rows");
        }
    }

    private async Task<int> ExecuteScalarIntAsync(
        SqlConnection sqlConnection, SqlTransaction sqlTransaction, string sqlQuery)
    {
        int resultToReturn;
        await using var sqlCommand = new SqlCommand(sqlQuery, sqlConnection, sqlTransaction);
        resultToReturn = Convert.ToInt32(await sqlCommand.ExecuteScalarAsync());
        return resultToReturn;
    }

    private async Task ExecuteNonQueryAsync(
        SqlConnection sqlConnection, SqlTransaction sqlTransaction, string sqlCommandText)
    {
        SqlCommand sqlCommand;
        await using (sqlCommand = new SqlCommand(sqlCommandText, sqlConnection, sqlTransaction) { CommandTimeout = 120 })
        {
            await sqlCommand.ExecuteNonQueryAsync();
        }
    }
}
