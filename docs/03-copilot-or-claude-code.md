# 3. Ask questions from Copilot or Claude Code

This guide connects GitHub Copilot in VS Code to the MCP server, so you can ask questions about the trading data in plain English. To use Claude Code, skip to [Use Claude Code instead](#use-claude-code-instead).

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

[Claude Code](https://code.claude.com/docs) can use the same server. It needs a paid Claude plan (Pro or above) or an Anthropic API key, so unlike the rest of the project it isn't free.

If the `claude` command isn't installed yet, install it, open a new terminal, and check it with `claude --version`:

Windows (PowerShell):

```powershell
irm https://claude.ai/install.ps1 | iex
```

macOS / Linux:

```sh
curl -fsSL https://claude.ai/install.sh | bash
```

The installer shows nothing while it downloads, which can take a minute or two, so give it time. The Claude Code extension for VS Code doesn't add the `claude` command, so install it even if you already use the extension. For other install options, see the [setup guide](https://code.claude.com/docs/en/setup).

If a new terminal still says `claude` isn't found, the install folder isn't on your PATH. Add it, then open another new terminal:

Windows (PowerShell):

```powershell
[Environment]::SetEnvironmentVariable("Path", [Environment]::GetEnvironmentVariable("Path", "User") + ";$env:USERPROFILE\.local\bin", "User")
```

macOS / Linux (on Linux with bash, use `~/.bashrc` instead of `~/.zshrc`):

```sh
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.zshrc
```

If it still isn't found, see [Fix your PATH](https://code.claude.com/docs/en/troubleshoot-install#command-not-found-claude-after-installation).

With the server still running, run this from the repository root, replacing `<token>` with the `MCP_TOKEN` value from `.env`:

```sh
claude mcp add --transport http trading http://localhost:5080/mcp --header "Authorization: Bearer <token>"
```

This adds the server only for the folder you run it in, so run it from the repository root, not your home folder. The output should end with `[project: <path to the repository>]`. Claude Code stores it in `.claude.json` in your home folder, not in the repository. Don't add `--scope project`: that writes the server, token included, to `.mcp.json` in the repository, where it could be committed.

Start Claude Code with `claude`, run `/mcp`, and check that `trading` shows as connected. A session that was already running, in the terminal or the VS Code extension, won't see the new server, so start a new one. The VS Code extension can use the server too, as long as VS Code has the repository folder open. Then try the questions above, including the delete. Like Copilot, Claude Code asks you to approve each tool call before it runs.

To see which servers Claude Code has for this folder and whether each one connects, or to remove `trading` so you can add it again (for example with a different token), run these from the repository root:

```sh
claude mcp list
claude mcp remove trading
```

## Troubleshooting

- **No trading tools in the tools picker:** check the server is still running and `http://localhost:5080/health` returns `{"status":"ok"}`, then restart the `trading` server in VS Code.
- **401 or "unauthorized" errors:** the token doesn't match. VS Code remembers the token you entered, so to enter it again, change the input's `id` (for example to `trading-mcp-token-2`) in both places in the configuration, then restart the `trading` server.
- **VS Code says authorisation is required but never asks for the token:** it's reusing a value you entered before. Change the input's `id` in both places, then start the server again. If it still doesn't ask, start it from the tools picker in Copilot Chat instead.
- **If all else fails, hard-code the token:** in your user configuration, replace `${input:trading-mcp-token}` with the `MCP_TOKEN` value, so the header reads `"Bearer <token>"`, and delete the `inputs` block. The token is then stored as plain text, but only in your user configuration, not in the repository, and it only works against your local server. Never do this in a `.vscode/mcp.json` file inside a repository, where it could be committed.
- **Claude Code shows `trading` as failed, with a 401:** the token doesn't match. Run `claude mcp remove trading`, then add it again with the right token.
- **Claude Code doesn't list `trading` in `/mcp` at all:** either the session started before you added the server, so start a new one, or you ran `claude mcp add` from a different folder. Run `claude mcp list` from the repository root to check, and add it again from there if it's missing.
- **Still stuck installing Claude Code:** if you have the Claude Code extension in VS Code, ask it to add the `claude` install folder to your PATH.
- **It works in MCP Inspector but not in VS Code:** the server is fine, so check the URL and header in the VS Code configuration.

## Other MCP hosts

The server isn't tied to VS Code. Any MCP host that can connect to a remote (HTTP) server and send an `Authorization` header can use it, with the same URL, `http://localhost:5080/mcp`, and the same token. See your host's documentation for how to add a server.

Next: optional [Azure deployment](04-azure-deployment.md) or [using a local model](05-local-llm.md).
