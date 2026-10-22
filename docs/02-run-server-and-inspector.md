# 2. Run the MCP server and test it

This guide configures the local server to connect to the database, checks its health endpoint, and makes a tool call using MCP Inspector.

Complete [the database and sample-data setup](01-docker-database.md) first.

## Configure and run the server

From the repository root, configure the connection string using the same `MCP_READER_PASSWORD` set in `.env` or `sql/02_security.sql`:

```sh
cd McpServer
dotnet user-secrets set "ConnectionStrings:Trading" "Server=localhost,1433;Database=TradingDemo;User ID=mcp_reader;Password=<MCP_READER_PASSWORD>;Encrypt=True;TrustServerCertificate=True;Connect Timeout=5"
```

Create a random token of at least 32 characters.

Windows (PowerShell):

```powershell
$bytes = [byte[]]::new(32)
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
-join ($bytes | ForEach-Object { $_.ToString("x2") })
```

macOS / Linux:

```bash
openssl rand -hex 32
```

Save the generated value temporarily in a password manager or other secure place, then configure it as a .NET user secret:

```sh
dotnet user-secrets set "Mcp:Tokens:inspector" "<paste-your-random-token>"
dotnet run --urls http://localhost:5080
```

The server runs in the foreground. Keep this terminal open. User-secrets are stored outside the repository; do not commit tokens or connection-string passwords.

## Check `/health`

Open [http://localhost:5080/health](http://localhost:5080/health). A healthy server returns:

```json
{"status":"ok"}
```

This endpoint checks that the server is running; it does not test database connectivity. The first successful tool call in Inspector verifies the database connection too.

## Connect with MCP Inspector

In a second terminal, start Inspector:

```sh
npx @modelcontextprotocol/inspector
```

In the Inspector browser window:

1. Select **Streamable HTTP**.
2. Set the URL to `http://localhost:5080/mcp`.
3. Configure the request header `Authorization` with the value `Bearer <your-random-token>` (replace the placeholder with the token saved above).
4. Connect and list the tools, then call `get_dataset_info` to verify the server can query the database.

The exact Inspector UI can vary by version. If it presents a bearer-token field, enter the token there; if it accepts custom headers, include the `Bearer ` prefix in the header value.

Next: [Connect VS Code and Copilot](03-vscode-copilot.md).
