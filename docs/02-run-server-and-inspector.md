# 2. Run the MCP server and test it with MCP Inspector

This guide starts the MCP server, then uses MCP Inspector to look at the server the way a model sees it: the tools, their descriptions, and the results and errors that come back.

Complete [guide 1](01-docker-database.md) first. The commands work unchanged in PowerShell, macOS and Linux unless both versions are shown. Run them from the repository root.

## 1. Run the server

```sh
dotnet run --project McpServer
```

When running locally (the Development environment), the server reads its settings from `.env`: it builds the database connection string from `MCP_READER_PASSWORD` and accepts `MCP_TOKEN` as a bearer token. It listens on `http://localhost:5080` and runs in the foreground, so keep this terminal open.

You should see:

```
Using settings from …/.env
Database check passed.
Now listening on: http://localhost:5080
```

If the database check fails, the warning says why, for example a wrong password or a stopped container. The server still starts, but tool calls will fail until it's fixed. If a required setting is missing, the server prints one line saying what to set, and exits.

To check the server is up, open [http://localhost:5080/health](http://localhost:5080/health) in a browser. It returns `{"status":"ok"}`. This only proves the server is running; the database check above covers the database.

## 2. Test it with MCP Inspector

[MCP Inspector](https://github.com/modelcontextprotocol/inspector) is the MCP project's official testing tool. It acts as an MCP client, like Copilot, but without a model: you choose which tool to call and with which arguments, and you see exactly what comes back. If something works in Inspector but not in Copilot, the problem is in the host's configuration, not the server.

Inspector needs [Node.js](https://nodejs.org/). This is the only part of the project that does.

### Use the web interface

In a second terminal, run:

```sh
npx @modelcontextprotocol/inspector
```

The first run downloads Inspector, then it opens in your browser. If the browser doesn't open, copy the address it prints, including any token in it.

**Connect to the server:**

1. Set the transport type to **Streamable HTTP**.
2. Set the URL to `http://localhost:5080/mcp`.
3. Add a header named `Authorization` with the value `Bearer <token>`, using the `MCP_TOKEN` value from `.env`. Include the word `Bearer` and the space.
4. Select **Connect**.

Inspector's screens change between versions, so the labels may differ slightly, but the four settings are always the same.

**Look at the tools.** Open the tools list. You should see all eight tools. Select one to see its description and arguments. These descriptions are exactly what a model reads when it decides which tool to call, which is why they're written as instructions for the model.

**Call some tools.** Run `get_dataset_info` with no arguments, then `get_top_movers` with `count` set to `3`, then `find_accounts` with `name` set to `Thompson`. The results come back as JSON, as a model would receive them.

**Try the guards.** Now give the tools bad arguments:

| Call | What comes back |
|---|---|
| `find_accounts` with `name` = `a` | An error: name must be at least 2 characters |
| `get_top_movers` with `count` = `50` | An error: count must be between 1 and 20 |
| `get_price_history` with `ticker` = `NOPE` | An error: unknown ticker, use `search_instruments` |

Each error is marked as an error and says what to do instead, so a model can correct itself rather than assume there's no data. The first two are checked by the server, the third by the stored procedure.

**See the protocol.** Inspector's history shows each request and response. A model's host does the same thing behind the scenes: `tools/list` to discover the tools, then `tools/call` for each call.

### Use the command line

Inspector also has a command-line mode, which is handy for quick checks. Replace `<token>` with the `MCP_TOKEN` value:

```sh
npx @modelcontextprotocol/inspector --cli http://localhost:5080/mcp --header "Authorization: Bearer <token>" --method tools/list
npx @modelcontextprotocol/inspector --cli http://localhost:5080/mcp --header "Authorization: Bearer <token>" --method tools/call --tool-name get_top_movers --tool-arg count=3
npx @modelcontextprotocol/inspector --cli http://localhost:5080/mcp --header "Authorization: Bearer <token>" --method tools/call --tool-name find_accounts --tool-arg name=a
```

The last one shows a rejected call: `"isError": true` and the message the model would get. Leave out the `--header` option and Inspector reports that authentication is required, because the server refuses calls without a valid token.

## 3. Check the audit log

Every tool call, including rejected ones, is written to `trading.AuditLog`: who called, which tool, the arguments, how long it took and whether it succeeded. To see the latest calls, replace `<SA_PASSWORD>` with the value from `.env`:

```sh
docker exec trading-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -C -U sa -P "<SA_PASSWORD>" -d TradingDemo -W -Q "SELECT TOP 5 OccurredAtUtc, Caller, ToolName, Success, Error FROM trading.AuditLog ORDER BY AuditId DESC"
```

You can also query it from the VS Code SQL Server extension, as in [guide 1](01-docker-database.md#see-it-for-yourself). The rejected calls show `Invalid arguments: …` in the `Error` column. These rows can never be changed or deleted, not even by `sa`: see [how the database is locked down](01-docker-database.md#how-the-database-is-locked-down).

## Configure the server with user-secrets

Reading `.env` is a convenience for the local walkthrough, and it only happens in Development. The standard way to give a .NET app its secrets during development is [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets): they're stored in your user profile, outside the repository. Anything set there takes priority over `.env`.

Use user-secrets to point the server at a different database, or to give each client its own token, so the audit log shows which client made each call. First generate a token.

Windows (PowerShell):

```powershell
[guid]::NewGuid().ToString('N') + [guid]::NewGuid().ToString('N')
```

macOS / Linux:

```bash
openssl rand -hex 32
```

Then set it, and optionally a connection string:

```sh
dotnet user-secrets set "Mcp:Tokens:inspector" "<token>" --project McpServer
dotnet user-secrets set "ConnectionStrings:Trading" "Server=localhost,1433;Database=TradingDemo;User ID=mcp_reader;Password=<MCP_READER_PASSWORD>;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5" --project McpServer
dotnet user-secrets list --project McpServer
```

Once any token is set in user-secrets, `MCP_TOKEN` in `.env` is no longer accepted. To go back to `.env`, run `dotnet user-secrets clear --project McpServer`. In production, the same settings come from App Service configuration instead: see [guide 4](04-azure-deployment.md).

## Troubleshooting

- **"No bearer tokens are set":** `MCP_TOKEN` is missing from `.env`. Add it from `.env.example`.
- **Inspector can't connect, or reports that authentication is required:** check the URL ends in `/mcp`, and that the header value is `Bearer ` followed by the token, exactly as it is in `.env` or user-secrets.
- **Port 5080 is in use:** run `dotnet run --project McpServer --urls http://localhost:5081`, and use that port in Inspector and VS Code.

Next: [Connect VS Code and Copilot](03-vscode-copilot.md).
