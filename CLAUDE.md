# CLAUDE.md

A read-only MCP server for SQL Server, written in C# / .NET 10. An AI assistant can query a synthetic trading database but cannot change it. This is an educational project, not production code, and all data is invented.

## Principle: pragmatism over idealism

Choose the simplest readable solution that solves the real problem (KISS). Don't add abstractions, layers, patterns or drift-proofing for hypothetical problems. A small manual upkeep cost, or a cheap test as a safety net, beats extra structure. Clean code matters (clear names, guard clauses, separation of concerns, small functions) only where it makes the code easier to follow.

## Layout

- `McpServer/` is the ASP.NET Core minimal API that hosts the MCP server. It contains tools, the repository, authentication, rate limiting and auditing.
- `DataGenerator/` is a console app that generates synthetic trading data and loads it into the database.
- `XTests/` holds the xUnit tests: unit tests in `DataGenerator/` and `McpServer/`, and integration tests against the Docker SQL Server in `Integration/`.
- `sql/` holds the database scripts. Run them in order: `00` creates the database, `01` the schema, `02` the security setup, `03` the stored procedures and final permissions.
- `docker/` and `docker-compose.yml` run the local SQL Server container and the `db-init` job.
- `docs/` holds the step-by-step guides that the README links to.

## Commands

```sh
dotnet build XTests/XTests.csproj                                    # builds all three projects
dotnet test XTests/XTests.csproj                                     # run the tests
dotnet test XTests/XTests.csproj --filter 'Category!=Integration'   # unit tests only, no Docker needed
dotnet test XTests/XTests.csproj --collect:"XPlat Code Coverage"     # with coverage
docker compose up -d --wait && docker compose run --rm db-init       # local database
cd DataGenerator && dotnet run -- --reset                            # load data (Docker: reads .env)
cd McpServer && dotnet run --urls http://localhost:5080              # run the server
```

## How a tool call works

Each tool is one line: `TradingTools` → `IAuditService.RunAsync` → `ITradingRepository` → a stored procedure in the `mcp` schema.

To add a tool:

1. Add a `CREATE OR ALTER PROCEDURE mcp.<Name>` to `sql/03_procedures.sql`. Return fixed columns, enforce a hard row limit and avoid dynamic SQL. For checks only the database can make (an unknown ticker or account), use `THROW 5000x, N'<message for the model>', 1;`. The server passes those messages to the model.
2. Add the method to `ITradingRepository` and `TradingRepository`, as a single `QueryAsync` call.
3. Add the tool to `McpServer/Tools/TradingTools.cs` with `[McpServerTool(Name = "snake_case", ReadOnly = true)]` and a clear `[Description]`. The model reads the descriptions to decide which tool to call, so they matter. Check the arguments at the top of the audit lambda with `ToolArguments.Require(...)`, with messages that tell the model what to do instead. Never let a bad argument quietly return an empty result.
4. Add unit tests in `XTests/McpServer/`, and a call to the new procedure in `XTests/Integration/When_Trading_Repository_Is_Queried.cs`. Test classes are named `When_<Something>_Is_<Done>` with the subject split into words (`When_Data_Loader_Is_Run`), and test methods are `it_should_<outcome>`. Integration tests carry `[Trait("Category", "Integration")]` and use `[IntegrationFact]`, which skips them when SQL Server isn't running.

## Security rules (do not weaken)

- The `mcp_reader` database user has `DENY` on the `trading` tables (`02_security.sql`) and `EXECUTE` on the `mcp` schema (`03_procedures.sql`), nothing else.
- Never add dynamic SQL, raw-query tools, or write operations on business tables.
- Never commit secrets. Local secrets live in .NET user-secrets and in the git-ignored `.env` file.
- Sensitive columns use Dynamic Data Masking, and every tool call is written to `trading.AuditLog`, an append-only ledger table. Never make it updatable.

## Docs conventions

- Readers are mid-level and senior developers following the guides on their own machines. Most are probably on Windows.
- Commands must work on Windows (PowerShell), macOS and Linux. Where they differ, show both, labelled `Windows (PowerShell):` first and `macOS / Linux:` second. Shell-neutral blocks use the `sh` fence.
- Commands must run on Windows PowerShell 5.1, not just PowerShell 7.
- Keep the data synthetic. No real companies, clients or prices.
