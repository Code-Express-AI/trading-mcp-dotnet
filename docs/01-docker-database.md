# 1. Start SQL Server and load the sample data

This guide starts the local SQL Server container, creates the trading database, and loads synthetic data. The sample data is invented and contains no real companies, clients, or market prices.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/), which includes Docker Compose. Make sure it is running before you start.
- [.NET SDK 10](https://dotnet.microsoft.com/download)

## Start the database with Docker

From the repository root, copy the sample environment file.

Windows (PowerShell):

```powershell
Copy-Item .env.example .env
```

macOS / Linux:

```bash
cp .env.example .env
```

Open `.env` in VS Code or any text editor and change both `SA_PASSWORD` and `MCP_READER_PASSWORD`. SQL Server requires strong passwords; follow the constraints noted in `.env.example`. Keep this file local: it is ignored by Git.

Start SQL Server and initialize its database, schema, security, and stored procedures:

```sh
docker compose up -d
docker compose run --rm db-init
```

`db-init` waits for SQL Server to be healthy. If the database already exists, it leaves it unchanged. To delete it and start again, run `docker compose run --rm -e RECREATE=1 db-init`.

Load the sample data:

```sh
cd DataGenerator
dotnet run -- --reset
cd ..
```

No connection string is needed. The generator finds the `.env` file and connects to the container as `sa` using `SA_PASSWORD`. You can still pass a connection string as the first argument if you want to. `--reset` deletes any existing data first, so you can run the same command again at any time.

Run `dotnet run -- --help` to see all the options, such as how many clients to create or how many years of prices to generate. The data ends yesterday, so it changes from day to day; add `--end-date=yyyy-MM-dd` to get exactly the same data every time.

## Use an existing SQL Server instead

You can use an existing SQL Server or Azure SQL Database instead of Docker:

1. For self-hosted SQL Server, run `sql/00_create_database.sql` as an administrator connected to `master`. For Azure SQL Database, create an empty database in Azure and skip this script.
2. Connect to `TradingDemo` and run `sql/01_schema.sql`, then `sql/02_security.sql`. In `02_security.sql`, replace the password placeholder with a strong password for `mcp_reader`; keep it for the server connection string.
3. Load the data as an administrator. Without Docker there's no `.env` file, so the connection string is required:
   ```sh
   cd DataGenerator
   dotnet run -- "Server=<server>;Database=TradingDemo;User ID=<admin>;Password=<admin-password>;Encrypt=True;TrustServerCertificate=True" --reset
   cd ..
   ```
   To keep the password out of your shell history, set the connection string in the `TRADING_DB_CONNECTION` environment variable instead and leave it off the command.
4. Run `sql/03_procedures.sql` as an administrator connected to `TradingDemo`.

Use a connection string appropriate for your SQL Server. `TrustServerCertificate=True` is for the local Docker container; do not use it for Azure SQL. For Azure-specific firewall and deployment steps, see [Deploy to Azure](04-azure-deployment.md).

## Useful Docker commands

```sh
docker compose down       # stop SQL Server; keep the database volume
docker compose down -v    # stop SQL Server and delete the database volume
```

On Windows, if SQL Server or SQL Server Express is already installed, it may already be using port 1433 and the container will fail to start. Stop that SQL Server service, then run `docker compose up -d` again.

On Apple Silicon, if the SQL Server container does not start, check the current SQL Server container guidance and the commented `platform: linux/amd64` option in `docker-compose.yml`. Emulation can be slower.

Next: [Run the MCP server and test it with MCP Inspector](02-run-server-and-inspector.md).
