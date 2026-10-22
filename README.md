# Secure, read-only MCP server for SQL Server: a real-world example in C# / .NET

[![build](https://github.com/Code-Express-AI/trading-mcp-dotnet/actions/workflows/build.yml/badge.svg)](https://github.com/Code-Express-AI/trading-mcp-dotnet/actions/workflows/build.yml)

### Project purpose

This project connects AI assistants, such as GitHub Copilot or Claude, to an existing SQL Server database (here, a fictional bank's stock trading system) using MCP. They can query the data, using a cloud or local model, but **can't change the database or any of its data**.

![Asking Copilot about the trading data](demo.gif)

### What is MCP?

MCP (Model Context Protocol) is the open standard that lets AI assistants discover and call tools in your own systems. In .NET, turning a method into a tool takes one attribute, `[McpServerTool]`. Making it safe to point at a real database is the hard part, and that's what this project shows.

### What you'll learn

How to:

- **Build a real MCP server:** eight read-only tools, with auditing, rate limiting and tests.
- **Lock it down with the principle of least privilege (PoLP):** the model can only call fixed tools, the database only runs stored procedures, and the audit log can't be changed, even by `sa`.

### Try it for free

Everything runs on your own machine. You'll need:

- [Git](https://git-scm.com/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (on Windows, use the default WSL 2 backend)
- [VS Code](https://code.visualstudio.com/) with [GitHub Copilot](https://github.com/features/copilot) (the free tier is enough)
- [Node.js](https://nodejs.org/) (optional: only needed to test the server with MCP Inspector)
- An [Azure account](https://azure.microsoft.com/free/) (optional: only for the Azure deployment guide)

Everything above is free for personal use: SQL Server runs as the free Developer edition in Docker, and the optional Azure deployment fits Azure's free tiers for App Service and Azure SQL Database.

## Quick start

These commands work unchanged in PowerShell, macOS and Linux. After step 1, run everything from the repository root. For an explanation of each step, see the [step-by-step guides](#step-by-step-guides), which also cover troubleshooting.

**1. Get the code:**

```sh
git clone https://github.com/Code-Express-AI/trading-mcp-dotnet.git
cd trading-mcp-dotnet
```

**2. Create your settings.** Copy the example file, then change the two passwords and the token in `.env`. Open it in VS Code: files starting with a dot are hidden in Finder and Explorer.

```sh
cp .env.example .env
```

**3. Start the database and load the data:**

```sh
docker compose up -d --wait
docker compose run --rm db-init
dotnet run --project DataGenerator
```

**4. Run the MCP server.** It reads its settings from `.env`:

```sh
dotnet run --project McpServer
```

**5. Connect.** In a second terminal, run `npx @modelcontextprotocol/inspector`, choose **Streamable HTTP**, enter `http://localhost:5080/mcp`, and add the header `Authorization: Bearer <token>`, using the `MCP_TOKEN` value from `.env`. To use Copilot instead, see [guide 3](docs/03-vscode-copilot.md).

## Videos

| # | Video | Guide |
|---|-------|-------|
| 1 | See it in action: Copilot answers questions from the database, and can't delete anything | |
| 2 | Set up SQL Server in Docker, and the database security behind it | [Guide 1](docs/01-docker-database.md) |
| 3 | Test the server with MCP Inspector, then connect Copilot | [Guides 2](docs/02-run-server-and-inspector.md) and [3](docs/03-vscode-copilot.md) |

Video links will be added as episodes are published. Deploying to Azure and running with a local model may follow, depending on viewer demand.

## Architecture

```mermaid
flowchart LR
    U([You]) --> H["Host<br/>(VS Code + Copilot)<br/>+ LLM"]
    H -- "MCP over HTTP<br/>+ bearer token" --> S["MCP server<br/>(.NET minimal API)<br/>auth, audit, rate limit"]
    S -- "Dapper:<br/>EXECUTE only" --> D[("SQL Server<br/>stored procedures only<br/>DENY on tables · masking<br/>ledger audit log")]
```

Many MCP database examples give the model a tool that runs any SQL it writes. This one doesn't: the model can only call eight fixed tools, each backed by a stored procedure. The model decides which tool to call; the host makes the call and returns the result. The security doesn't rely on the model behaving: even if it's tricked into asking for something else, the server and the database refuse. And if it makes too many calls in quick succession, a rate limit stops it.

- **Server:** ASP.NET Core minimal API and Dapper
- **Database:** SQL Server, accessed only through stored procedures
- **Authentication:** named bearer tokens locally, Microsoft Entra ID in production
- **Secrets:** .NET user-secrets and the git-ignored `.env` file, never in source control

## The tools

| Tool | Example question |
|---|---|
| `get_dataset_info` | What data is there, and what's the latest price date? |
| `search_instruments` | Which pharma companies are listed? |
| `get_price_history` | How has Brightwater Pharma's share price moved this year? |
| `get_top_movers` | What were the biggest movers on the last trading day? |
| `find_accounts` | Find the accounts for a client called Tillman. |
| `list_trades` | What did that account trade last month? |
| `get_positions` | What does that account hold, and is it up or down? |
| `get_portfolio_summary` | Summarise that account's portfolio by sector. |

You ask in plain English, and the model picks the tools, often several for one question. For example, "Do any of Tillman's accounts hold yesterday's three biggest losers?" uses `find_accounts`, `get_top_movers` and `get_positions`.

## Where to look in the code

- [TradingTools.cs](McpServer/Tools/TradingTools.cs): the eight tools, their descriptions and argument checks
- [03_procedures.sql](sql/03_procedures.sql): the stored procedures, the security boundary
- [AuditService.cs](McpServer/Audit/AuditService.cs): auditing, and errors written for the model
- [McpServerApplication.cs](McpServer/McpServerApplication.cs): the whole server, set up on one page

## Step-by-step guides

The guides explain each step and why it works the way it does, and cover testing, troubleshooting and the optional setups.

1. [Start SQL Server and load the synthetic trading data](docs/01-docker-database.md)
2. [Run the MCP server and test it with MCP Inspector](docs/02-run-server-and-inspector.md)
3. [Connect VS Code and Copilot, then ask questions](docs/03-vscode-copilot.md)
4. Optional: [Deploy the database and MCP server to Azure](docs/04-azure-deployment.md)
5. Optional: [Use a local LLM from VS Code](docs/05-local-llm.md)

## Tests

Unit tests need nothing installed. Integration tests run against the local Docker SQL Server and prove the security holds: the MCP login can't read or change the tables directly, and nobody, not even `sa`, can change the audit log. If SQL Server isn't running, they're skipped. GitHub Actions runs every test on every push.

```sh
dotnet test XTests/XTests.csproj                                     # all tests
dotnet test XTests/XTests.csproj --filter 'Category!=Integration'   # unit tests only
dotnet test XTests/XTests.csproj --collect:"XPlat Code Coverage"     # with coverage
```

## Security design

Each layer grants only what's needed, and holds even if the one above it fails.

| Layer | Protection |
|---|---|
| Who can call | Named bearer tokens locally, [Microsoft Entra ID](docs/entra-setup.md) in production. Only the expected host names are accepted, which blocks DNS-rebinding attacks |
| How often | A rate limit for each caller |
| What they can ask | Eight fixed, read-only tools, with every argument checked |
| What the database allows | `EXECUTE` on the stored procedures and nothing else: `DENY` on every table, fixed columns, row limits, no dynamic SQL |
| What leaks | Account numbers and emails are masked, and database errors never reach the model |
| What's recorded | Every call, in an append-only ledger table that even `sa` can't change, and that SQL Server can verify hasn't been tampered with |

Before using this pattern with a real database, see [Before you go to production](docs/04-azure-deployment.md#before-you-go-to-production).

## Tested with

| Component | Version | Date checked |
|-----------|---------|--------------|
| .NET SDK | 10.0.x | 22-Oct-2026 |
| ModelContextProtocol.AspNetCore | 2.3.0 | 22-Oct-2026 |
| SQL Server container | 2022-latest | 22-Oct-2026 |
| Host / model | VS Code / Copilot | 15-Oct-2026 |

The MCP SDK and its authorization support are changing quickly. If something doesn't work, check the [C# SDK's issues](https://github.com/modelcontextprotocol/csharp-sdk/issues) for known problems.

## Licence

MIT. See [LICENSE](LICENSE).
