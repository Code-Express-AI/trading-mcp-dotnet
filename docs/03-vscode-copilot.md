# 3. Ask questions from VS Code and Copilot

This guide connects VS Code to the local MCP server so Copilot Chat can use its read-only trading tools.

Complete [the database setup](01-docker-database.md) and [run the MCP server](02-run-server-and-inspector.md) first. Keep the server running at `http://localhost:5080`.

## Add the MCP server to VS Code

In VS Code, open the Command Palette (`Ctrl+Shift+P` on Windows and Linux, `Cmd+Shift+P` on macOS) and run **MCP: Open User Configuration**. Add the following VS Code-format configuration, replacing the existing contents only if you do not need them:

```json
{
  "inputs": [
    {
      "type": "promptString",
      "id": "trading-mcp-token",
      "description": "Bearer token for the local trading MCP server",
      "password": true
    }
  ],
  "servers": {
    "trading": {
      "type": "http",
      "url": "http://localhost:5080/mcp",
      "headers": {
        "Authorization": "Bearer ${input:trading-mcp-token}"
      }
    }
  }
}
```

Using user configuration keeps the token out of the repository. VS Code prompts for it when the server needs the value. Enter the same token configured in .NET user-secrets. For current configuration options, see the [VS Code MCP server documentation](https://code.visualstudio.com/docs/agent-customization/mcp-servers).

After saving, connect to or restart the `trading` server from VS Code's MCP server controls. This connects VS Code to the already-running .NET service; it does not start that service. If prompted, confirm that you trust the server. Open Copilot Chat in **Agent** mode and confirm that the trading tools are available in the tools picker.

## Try a question

For example:

> What were the five biggest daily movers on the latest trading day? Which ones were losers?

Other prompts:

- “What data is available, and what is the latest price date?”
- “Find accounts for a client named MacGyver.”
- “Summarise this account's positions by market value and unrealised profit or loss.”

The available tools only read the curated data exposed by stored procedures; the MCP server does not provide arbitrary SQL or write operations.

If no tools appear, confirm the local server is running, check `/health`, verify the URL and token, and restart the MCP server connection in VS Code. Use [MCP Inspector](02-run-server-and-inspector.md) to distinguish a server/database issue from a VS Code configuration issue.

## Other MCP hosts

The server isn't tied to VS Code. Any MCP host that can connect to a remote (HTTP) server and send an `Authorization` header can use it, with the same URL, `http://localhost:5080/mcp`, and the same token. See your host's documentation for how to add a server.

Next: optional [Azure deployment](04-azure-deployment.md) or [using a local LLM](05-local-llm.md).
