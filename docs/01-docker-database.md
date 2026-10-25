# 1. Set up the database

This guide starts SQL Server in Docker, creates the trading database, and loads it with sample data: a fictional bank with clients, accounts, trades and daily share prices. None of it is real. It then shows how the database is locked down, and lets you try it for yourself.

The commands are the same as steps 2 and 3 of the README's [Quick start](../README.md#quick-start), and work unchanged in PowerShell, macOS and Linux. Run them from the repository root, with Docker Desktop running.

## 1. Create your settings

```sh
cp .env.example .env
```

Open `.env` in VS Code (files starting with a dot are hidden in Finder and Explorer) and change all three values:

| Setting | Used for |
|---|---|
| `SA_PASSWORD` | The SQL Server administrator. The DataGenerator uses it to load the data |
| `MCP_READER_PASSWORD` | `mcp_reader`, the locked-down login the MCP server uses |
| `MCP_TOKEN` | The bearer token MCP clients send to the server, 32 characters or more |

SQL Server requires passwords of at least 8 characters, with at least three of: uppercase, lowercase, digits and symbols. Avoid the characters listed in `.env.example`. `.env` is ignored by Git, so your passwords never leave your machine.

## 2. Start SQL Server

```sh
docker compose up -d --wait
```

This starts SQL Server 2022 in the background as a container called `trading-sqlserver`, listening on port 1433. The first time, it downloads the SQL Server image (over 2 GB on disk), which can take a few minutes. The database files live in a Docker volume, so your data survives restarts. `--wait` returns once SQL Server reports that it's healthy.

You should see `Container trading-sqlserver Healthy`.

## 3. Create the database

```sh
docker compose run --rm db-init
```

`db-init` is a one-off job that runs the four scripts in the `sql` folder, in order:

| Script | Creates |
|---|---|
| `00_create_database.sql` | The empty `TradingDemo` database |
| `01_schema.sql` | The tables and views, including masked columns and the ledger audit table |
| `02_security.sql` | The `mcp_reader` user, denied access to every table |
| `03_procedures.sql` | The stored procedures, and permission for `mcp_reader` to run them |

You should see each script run, then `Done. Database 'TradingDemo' is ready…`.

If the database already exists, `db-init` leaves it alone. To delete it and start again, run `docker compose run --rm -e RECREATE=1 db-init`.

## 4. Load the data

```sh
dotnet run --project DataGenerator
```

The DataGenerator reads `SA_PASSWORD` from `.env`, so it needs no connection string. By default it creates 50 companies, three years of daily prices, 200 clients with one to three accounts each, and a realistic history of trades. Prices follow a random walk with sector-specific growth and volatility, so charts and daily movers look believable.

You should see a row count for each table, then `Done. … open positions across … accounts.`

Some useful options:

```sh
dotnet run --project DataGenerator -- --help
dotnet run --project DataGenerator -- --reset
dotnet run --project DataGenerator -- --reset --end-date=2026-06-30
```

- The first `--` tells `dotnet run` that everything after it belongs to the DataGenerator, not to `dotnet`.
- `--reset` deletes the existing data and loads new data. Without it, the generator refuses to touch a database that already has data.
- The data ends yesterday, so it changes from day to day. `--end-date` makes it repeatable: the same end date always produces exactly the same data.

## How the database is locked down

The MCP server connects as `mcp_reader`, and `mcp_reader` can do almost nothing. Each layer grants only what's needed, following the principle of least privilege.

**No access to the tables.** `02_security.sql` denies `mcp_reader` every permission on the tables: no `SELECT`, `INSERT`, `UPDATE` or `DELETE`. Whatever the server code tries, or a model tricked into asking for something it shouldn't, the database refuses.

**Stored procedures are the only way in.** `03_procedures.sql` grants `mcp_reader` one thing: `EXECUTE` on each stored procedure, one by one. We don't grant on the whole `mcp` schema, because then a procedure added there by mistake would be callable straight away. Each procedure returns fixed columns and a limited number of rows, and none of them builds SQL from text. So the procedures are a hard limit on what anyone can ask for.

**Ownership chaining makes it work.** The procedures can read tables that `mcp_reader` can't, because the procedures and the tables have the same owner. SQL Server then skips the permission check on the tables. Dynamic SQL (`EXEC(@sql)`) would break that chain, which is one more reason the procedures don't use it.

**Masking.** Account numbers and email addresses are masked in the table definitions. `mcp_reader` doesn't have the `UNMASK` permission, so it only ever sees values like `ACCXXXXX149`. That holds even through the stored procedures, because ownership chaining skips the table permission checks but not masking: SQL Server checks whether the caller has `UNMASK`, not the procedure's owner.

On an existing database you don't need to recreate the tables. You can add a mask to a column that's already there:

```sql
ALTER TABLE trading.Account
    ALTER COLUMN AccountNumber ADD MASKED WITH (FUNCTION = 'partial(3,"XXXXX",3)');
```

**No internal IDs leave the database.** Every table has a fast integer primary key for joins, and a random GUID key next to it (`AccountKey`, `ClientKey` and so on). The procedures only return the GUID, and only when another tool needs it: `find_accounts` returns an `AccountKey` because `get_positions` takes one. Sequential numbers would tell anyone with a stolen token how many accounts we have, and let them walk through every one from 1 upwards. A random key gives nothing away. The integration tests fail if a procedure ever returns a column ending in `Id`.

**A tamper-proof audit log.** The MCP server records every tool call in `trading.AuditLog`. It's an append-only ledger table: rows can be added but never changed or deleted, not even by `sa`. SQL Server also keeps a hash chain through every row, so tampering with the data files is detected.

**Contained users.** `mcp_reader` exists only inside the database, with its own password. That's how Azure SQL Database works, so the same scripts run locally and in Azure.

## See it for yourself

Connect to the database twice, first as `sa` and then as `mcp_reader`, and run the same kind of queries. The easiest way is the [SQL Server (mssql) extension](https://marketplace.visualstudio.com/items?itemName=ms-mssql.mssql) for VS Code. On Windows, SQL Server Management Studio works too. Use these connection details:

| Setting | Value |
|---|---|
| Server | `localhost,1433` |
| Authentication | SQL Login |
| User name and password | `sa` and `SA_PASSWORD`, then `mcp_reader` and `MCP_READER_PASSWORD` |
| Database | `TradingDemo` |
| Trust server certificate | Yes |

**As `sa`**, you can see everything:

```sql
SELECT TOP 5 FullName, Email FROM trading.Client;     -- real email addresses
EXEC mcp.FindAccounts @Name = N'Thompson';            -- full account numbers
DELETE FROM trading.AuditLog;                         -- refused: append-only ledger table
```

**As `mcp_reader`**, you can only run the stored procedures, and they set the limits:

```sql
SELECT TOP 5 FullName, Email FROM trading.Client;     -- refused: no permission on the table
EXEC mcp.FindAccounts @Name = N'Thompson';            -- works, but account numbers are masked
EXEC mcp.GetTopMovers @Count = 100;                   -- returns at most 20 rows
EXEC mcp.GetPriceHistory @Ticker = N'NOPE';           -- refused: "Unknown ticker..."
DELETE FROM trading.Trade;                            -- refused: no permission on the table
```

These rules are also checked automatically by the integration tests in `XTests/Integration`.

To use the command line instead, run `sqlcmd` inside the container, replacing the password:

```sh
docker exec -it trading-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -C -d TradingDemo -U mcp_reader -P "<MCP_READER_PASSWORD>"
```

Type a query, then `GO` on its own line to run it, and `exit` to leave.

## Troubleshooting

- **Port 1433 is already in use** (common on Windows): SQL Server or SQL Server Express is probably installed on your machine. Stop that SQL Server service, then run `docker compose up -d --wait` again.
- **The container is unhealthy:** the usual cause is an `SA_PASSWORD` that's too weak. Check with `docker compose logs sqlserver`.
- **Login failed after changing `SA_PASSWORD`:** the password only applies when the container is first created. Run `docker compose down -v` to delete the database, then start again from step 2.
- **Apple Silicon Macs:** SQL Server only ships x64 images. Docker Desktop runs it under Rosetta automatically; it's a little slower, but nothing needs changing.

The DataGenerator explains most problems itself, for example a wrong password or a container that isn't running.

## Useful Docker commands

```sh
docker compose down       # stop SQL Server and keep the data
docker compose down -v    # stop SQL Server and delete the data
```

Deleting the container alone keeps the data, because it lives in the Docker volume. To start again from scratch, delete the volume too, then repeat steps 2 to 4:

```sh
docker compose down -v
docker compose up -d --wait
docker compose run --rm db-init
dotnet run --project DataGenerator
```

## Use an existing SQL Server instead

You can use an existing SQL Server 2022 (or later) or Azure SQL Database instead of Docker. Earlier versions won't work, because the audit table is a ledger table.

1. For SQL Server, run `sql/00_create_database.sql` as an administrator connected to `master`. For Azure SQL Database, create an empty database in Azure and skip this script.
2. Connect to `TradingDemo` and run `sql/01_schema.sql`, `sql/02_security.sql` and `sql/03_procedures.sql`, in that order. In `02_security.sql`, replace the password placeholder with a strong password for `mcp_reader`, and keep it for the MCP server's connection string.
3. Load the data as an administrator. Without Docker there's no `.env` file, so pass the connection string:
   ```sh
   dotnet run --project DataGenerator -- "Server=<server>;Database=TradingDemo;User ID=<admin>;Password=<admin-password>;Encrypt=True"
   ```
   To keep the password out of your shell history, put the connection string in the `TRADING_DB_CONNECTION` environment variable instead and leave it off the command.

Add `TrustServerCertificate=True` only for a local server with a self-signed certificate; never use it for Azure SQL. For Azure firewall and deployment steps, see [Deploy to Azure](04-azure-deployment.md).

Next: [Run the MCP server and test it with MCP Inspector](02-run-server-and-inspector.md).
