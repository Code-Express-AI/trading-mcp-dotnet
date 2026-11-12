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
docker compose up -d --wait                                          # start the local database
docker compose run --rm db-init                                      # create the database
dotnet run --project DataGenerator -- --reset                        # load data (Docker: reads .env)
dotnet run --project McpServer                                       # run the server (Development: reads .env)
```

## How a tool call works

Each tool checks its arguments, then calls `IAuditService.RunAsync` → `ITradingRepository` → one stored procedure in the `mcp` schema. Every connection the repository opens sets the caller, read-only, in the session context: `caller` (the readable name) and `caller_key` (the token name, or the Entra `oid`), which row-level security uses.

To add a tool:

1. Add a `CREATE OR ALTER PROCEDURE mcp.<Name>` to `sql/03_procedures.sql`, and a `GRANT EXECUTE ON mcp.<Name> TO mcp_reader;` line to the list at the end of the file. Start it with the two lines every read procedure has, which record the call with `trading.RecordProcedureCall`. Return fixed columns (GUID keys, never integer IDs), enforce a hard row limit and avoid dynamic SQL. For checks only the database can make (an unknown ticker or account), use `THROW 5000x, N'<message for the model>', 1;`. The server passes those messages to the model.
2. Add the method to `ITradingRepository` and `TradingRepository`, as a single `QueryAsync` call.
3. Add the tool to `McpServer/Tools/TradingTools.cs` with `[McpServerTool(Name = "snake_case", ReadOnly = true)]` and a clear `[Description]`. The model reads the descriptions to decide which tool to call, so they matter. Check the arguments at the top of the audit lambda with `ToolArguments.Require(...)`, with messages that tell the model what to do instead. Pass free-text arguments through `ToolArguments.RequirePlainText`, and allow only letters and digits in tickers: the SQL collation ignores some characters that .NET counts. Never let a bad argument quietly return an empty result.
4. Add unit tests in `XTests/McpServer/`, and a call to the new procedure in `XTests/Integration/When_Trading_Repository_Is_Queried.cs`. Add its `GRANT EXECUTE` line to the expected list in `XTests/Integration/When_Mcp_Reader_Permissions_Are_Listed.cs`.

## Tests

- Test classes are named `When_<Something>_Is_<Done>`, with the subject split into words (`When_Data_Loader_Is_Run`). Test methods are `it_should_<outcome>`.
- Integration tests carry `[Trait("Category", "Integration")]` and use `[IntegrationFact]`, which skips them when SQL Server isn't running. On CI, `When_Ci_Build_Is_Run` fails instead, so a broken database step can't hide behind skipped tests.
- An integration test that needs the schema in a particular state builds its own throwaway database from the real scripts, as `When_Rows_Are_Filtered_By_Caller` does, rather than changing `TradingDemo`.
- xUnit v3 runs on the classic VSTest runner (`IsTestingPlatformApplication=false` in `XTests.csproj`). Don't switch to Microsoft Testing Platform: it breaks `dotnet test`, the `--filter` syntax, coverage and CI.

## Gotchas

- Dapper can't map SQL `DATE` columns to `DateOnly` record parameters, so the models use `DateTime`.
- `TradingDemo` is a contained database, so catalog names (`sys.*`, `OBJECT_NAME()`) and string literals have different collations. Joining them in SQL fails with a collation conflict; format them in C# instead.

## Security rules (do not weaken)

- The `mcp_reader` database user has `DENY` on the `trading` tables (`02_security.sql`) and `EXECUTE` on each `mcp` procedure, granted one by one at the end of `03_procedures.sql`, nothing else. Never grant on the whole schema: a procedure added to `mcp` by mistake would become callable.
- Never add dynamic SQL, raw-query tools, or write operations on business tables.
- Row-level security (`02_security.sql`) filters `Client`, `Account` and `Trade` by `trading.CallerClient`. A new table holding client data gets a filter predicate in the same policy. Only `db_owner` is exempt; never exempt `mcp_reader`. A procedure that takes an `accountKey` looks it up in `Account`, so a hidden account gets the same "Unknown accountKey" as a missing one.
- Integer IDs never leave the database. Every table with an `IDENTITY` key also has a random GUID `<Name>Key` (`DEFAULT NEWID()`, not `NEWSEQUENTIALID()`). Procedures take and return the key, and return it only when another tool needs it as input. An integration test fails if a procedure returns a column ending in `Id`.
- `OriginCheckMiddleware` runs before authentication and refuses any `Origin` header not in `Mcp:AllowedOrigins` (empty by default), as the MCP specification requires. If a host sends an `Origin`, add that exact origin to the list. Never use wildcards or skip the check.
- Never commit secrets. Local secrets live in .NET user-secrets and in the git-ignored `.env` file.
- The server reads `.env` only in Development, and only for settings not configured elsewhere. Never let it read `.env` in Production.
- Sensitive columns use Dynamic Data Masking, and every tool call is written to `trading.AuditLog`, an append-only ledger table, by the server (`Source` 'server', through `mcp.WriteAudit`) and by the procedure itself before it returns data (`Source` 'procedure', through `trading.RecordProcedureCall`). Never make it updatable, never let `mcp.WriteAudit` write a 'procedure' row, and never let a read procedure skip `trading.RecordProcedureCall` or run inside a transaction.

## Docs conventions

- Readers are mid-level and senior developers following the guides on their own machines. Most are probably on Windows.
- Commands must work on Windows (PowerShell), macOS and Linux. Where they differ, show both, labelled `Windows (PowerShell):` first and `macOS / Linux:` second. Shell-neutral blocks use the `sh` fence.
- Commands must run on Windows PowerShell 5.1, not just PowerShell 7.
- The README Quick start and the guides use identical commands, run from the repository root with `--project`.
- Node.js is optional: it's only needed for MCP Inspector, and the docs must say so.
- Headings use sentence case.
- Keep the data synthetic. No real companies, clients or prices.
- The docs and videos use Thompson as the example client. `SeedData` guarantees Giovanny (34) and Tessie (53) Thompson for any seed, with at least two accounts each, visible to every token. Other client names come from Bogus and depend on the seed and the Bogus version, so never tell readers to search for one. Guide 1's queries show them how to find names in their own data. Made-up names that only illustrate a point ("Ann Lee", O'Connell) are fine.
