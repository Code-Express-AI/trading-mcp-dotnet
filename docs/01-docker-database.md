# 1. Start SQL Server and load the sample data

This guide starts the local SQL Server container, creates the trading database, and loads synthetic data. The sample data is invented and contains no real companies, clients, or market prices.

## Prerequisites

- Docker Desktop with Docker Compose
- [.NET SDK 10](https://dotnet.microsoft.com/download)

## Start the database with Docker

From the repository root, copy the sample environment file:

```bash
cp .env.example .env
```

In PowerShell, use:

```powershell
Copy-Item .env.example .env
```

Edit `.env` and change both `SA_PASSWORD` and `MCP_READER_PASSWORD`. SQL Server requires strong passwords; follow the constraints noted in `.env.example`. Keep this file local: it is ignored by Git.

Start SQL Server and initialize its database, schema, security, and stored procedures:

```sh
docker compose up -d
docker compose run --rm db-init
```

`db-init` waits for SQL Server to be healthy. If the database already exists, it leaves it unchanged.

Load the sample data as the SQL Server administrator:

```sh
cd DataGenerator
dotnet run -- "Server=localhost,1433;Database=TradingDemo;User ID=sa;Password=<SA_PASSWORD>;Encrypt=True;TrustServerCertificate=True" --reset
cd ..
```

Replace `<SA_PASSWORD>` with the value from `.env`. `--reset` clears and reloads the generated data. The generator also accepts options such as `--seed=42`, `--instruments=50`, `--clients=200`, `--years=3`, and `--trades-per-account=40`.

## Use an existing SQL Server instead

You can use an existing SQL Server or Azure SQL Database instead of Docker:

1. For self-hosted SQL Server, run `sql/00_create_database.sql` as an administrator connected to `master`. For Azure SQL Database, create an empty database in Azure and skip this script.
2. Connect to `TradingDemo` and run `sql/01_schema.sql`, then `sql/02_security.sql`. In `02_security.sql`, replace the password placeholder with a strong password for `mcp_reader`; keep it for the server connection string.
3. Load the data as an administrator:
   ```sh
   cd DataGenerator
   dotnet run -- "Server=<server>;Database=TradingDemo;User ID=<admin>;Password=<admin-password>;Encrypt=True;TrustServerCertificate=True" --reset
   cd ..
   ```
4. Run `sql/03_procedures.sql` as an administrator connected to `TradingDemo`.

Use a connection string appropriate for your SQL Server. `TrustServerCertificate=True` is for the local Docker container; do not use it for Azure SQL. For Azure-specific firewall and deployment steps, see [Deploy to Azure](04-azure-deployment.md).

## Useful Docker commands

```sh
docker compose down       # stop SQL Server; keep the database volume
docker compose down -v    # stop SQL Server and delete the database volume
```

On Apple Silicon, if the SQL Server container does not start, check the current SQL Server container guidance and the commented `platform: linux/amd64` option in `docker-compose.yml`. Emulation can be slower.

Next: [Run the MCP server and test it with MCP Inspector](02-run-server-and-inspector.md).
