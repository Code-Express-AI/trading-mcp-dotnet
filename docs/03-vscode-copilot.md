# 3. Ask questions from VS Code and Copilot

This guide connects GitHub Copilot in VS Code to the MCP server, so you can ask questions about the trading data in plain English. To use Claude Code instead, see [Use Claude Code instead](#use-claude-code-instead).

Complete [guide 1](01-docker-database.md), and keep the server from [guide 2](02-run-server-and-inspector.md) running at `http://localhost:5080`.

## 1. Add the server to VS Code

Open the Command Palette (`Ctrl+Shift+P` on Windows and Linux, `Cmd+Shift+P` on macOS) and run **MCP: Open User Configuration**. Add the `trading` server below. If the file already lists other servers, add `trading` alongside them.

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

The token isn't written in the file. VS Code asks for it the first time it connects and stores it securely. Enter the `MCP_TOKEN` value from `.env`, or the token you set in user-secrets. Because this is your user configuration, not a file in the repository, nothing secret can be committed by accident. For all the options, see the [VS Code MCP documentation](https://code.visualstudio.com/docs/agent-customization/mcp-servers).

## 2. Connect

Save the file, then start the `trading` server from VS Code's MCP server list (or the **Start** link that appears above it in the file). This only connects VS Code to the server that's already running; it doesn't start the .NET app. If VS Code asks whether you trust the server, confirm.

Open Copilot Chat, switch it to **Agent** mode, and open the tools picker. The eight trading tools should be listed and selected.

## 3. Ask questions

Try these:

- "What data is available, and what's the latest price date?"
- "What were the five biggest movers on the last trading day? Which ones were losers?"
- "How has Brightwater Pharma's share price moved this year?"
- "Find the accounts for clients called Thompson, and summarise one of their portfolios by sector."
- "Do any of Thompson's accounts hold yesterday's three biggest losers?"

The last two need several tools in a row. Copilot shows each tool call as it makes it, so you can see how it breaks the question down.

Now ask it to change something:

- "Delete all of Tessie Thompson's trades."

It can't. None of the tools can change data, and even if one tried, the database login has no permission to.

Copilot may ask you to approve each tool call before it runs. That's VS Code's own safeguard, and a good one to leave switched on.

## Use Claude Code instead

[Claude Code](https://code.claude.com/docs) can use the same server. It needs a Claude subscription or an Anthropic API key, so unlike the rest of the project it isn't free.

With the server still running, run this from the repository root, replacing `<token>` with the `MCP_TOKEN` value from `.env`:

```sh
claude mcp add --transport http trading http://localhost:5080/mcp --header "Authorization: Bearer <token>"
```

This adds the server for this project only. Claude Code stores it in `.claude.json` in your home folder, not in the repository. Don't add `--scope project`: that writes the server, token included, to `.mcp.json` in the repository, where it could be committed.

Start Claude Code with `claude`, run `/mcp`, and check that `trading` shows as connected. Then try the questions above, including the delete. Like Copilot, Claude Code asks you to approve each tool call before it runs.

## Troubleshooting

- **No trading tools in the tools picker:** check the server is still running and `http://localhost:5080/health` returns `{"status":"ok"}`, then restart the `trading` server in VS Code.
- **401 or "unauthorized" errors:** the token doesn't match. VS Code remembers the token you entered, so to enter it again, change the input's `id` (for example to `trading-mcp-token-2`) in both places in the configuration, then restart the `trading` server.
- **Claude Code shows `trading` as failed, with a 401:** the token doesn't match. Run `claude mcp remove trading`, then add it again with the right token.
- **It works in MCP Inspector but not in VS Code:** the server is fine, so check the URL and header in the VS Code configuration.

## Other MCP hosts

The server isn't tied to VS Code. Any MCP host that can connect to a remote (HTTP) server and send an `Authorization` header can use it, with the same URL, `http://localhost:5080/mcp`, and the same token. See your host's documentation for how to add a server.

Next: optional [Azure deployment](04-azure-deployment.md) or [using a local model](05-local-llm.md).
