# 3. Ask questions from Copilot or Claude Code

This guide connects GitHub Copilot in VS Code to the MCP server, so you can ask questions about the trading data in plain English. To use Claude Code instead of Copilot, skip to [Use Claude Code instead](#use-claude-code-instead).

Complete guides [1](01-docker-database.md) and [2](02-run-server-and-inspector.md).

**Stop the server from guide 2 before you start** (`Ctrl+C` in its terminal). Add the configuration to VS Code first, then start the server. That's the order that works reliably. If you add the server to Copilot or Claude Code while it's running, you may run into connection problems.

## 1. Add the server to VS Code

Open the Command Palette (`Ctrl+Shift+P` on Windows and Linux, `Cmd+Shift+P` on macOS) and run **MCP: Open User Configuration**. Add the `trading` server below. If the file already lists other servers, add `trading` alongside them.

```json
{
  "servers": {
    "trading": {
      "type": "http",
      "url": "http://localhost:5080/mcp",
      "headers": {
        "Authorization": "Bearer <token>"
      }
    }
  }
}
```

Replace `<token>` with the `MCP_TOKEN` value from `.env`, or the token you set in user-secrets. The token is stored as plain text, but only in your user configuration, not in the repository, and it only works against your local server. Never put it in a `.vscode/mcp.json` file inside a repository, where it could be committed.

VS Code can prompt for the token instead, using an `inputs` block and `${input:...}`, but the Copilot Agents window doesn't support input variables. For a local demo, we keep it simple and write the token in. For all the options, see the [VS Code MCP documentation](https://code.visualstudio.com/docs/agent-customization/mcp-servers).

Save the file.

## 2. Start the server

From the repository root, start the server and leave it running in its own terminal:

```sh
dotnet run --project McpServer
```

## 3. Connect

Open Copilot Chat and switch it to **Agent** mode. If VS Code asks whether you trust the `trading` server, confirm. Then ask:

- "List the tools on the trading MCP server, then call one to check it works. Use the server, not the code in this repository."

Copilot can read the repository, so if you just ask what tools there are, it may answer from the source code instead of calling the server. That answer looks right even when nothing is connected. Check that Copilot shows a tool call. The eight trading tools should also be listed and selected in the tools picker.

## 4. Ask questions

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

If the server is running, stop it first with Ctrl+C. Otherwise Claude Code can keep using an old token. Then run this from the repository root, replacing `<token>` with the `MCP_TOKEN` value from `.env`:

```sh
claude mcp add --transport http trading http://localhost:5080/mcp --header "Authorization: Bearer <token>"
```

This adds the server only for the folder you run it in, so run it from the repository root, not your home folder. The output should end with `[project: <path to the repository>]`. Claude Code stores it in `.claude.json` in your home folder, not in the repository. Don't add `--scope project`: that writes the server, token included, to `.mcp.json` in the repository, where it could be committed.

Start the server again (`dotnet run --project McpServer`). Then start Claude Code with `claude`, run `/mcp`, and check that `trading` shows as connected. A session that was already running, in the terminal or the VS Code extension, won't see the new server, so start a new one. The VS Code extension can use the server too, as long as VS Code has the repository folder open. Then try the questions above, including the delete. Like Copilot, Claude Code asks you to approve each tool call before it runs.

To see which servers Claude Code has for this folder and whether each one connects, or to remove `trading` so you can add it again (for example with a different token), run these from the repository root. Stop the server before you remove and add `trading`, and start it again afterwards.

```sh
claude mcp list
claude mcp remove trading
```

## Troubleshooting

### VS Code / Copilot

- **No trading tools in the tools picker:** check the server is still running and `http://localhost:5080/health` returns `{"status":"ok"}`, then restart the `trading` server in VS Code.
- **401 or "unauthorized" errors:** the token doesn't match. Check that the header reads `Bearer ` followed by the exact `MCP_TOKEN` value, with no `<` or `>` left in, then restart the `trading` server.
- **"Input and command variables are not supported by the Copilot harness":** the configuration still uses `${input:...}`. Replace it with the token itself, as shown in step 1, and delete the `inputs` block.
- **"Failed to read configuration from .../.copilot/mcp-config.json: mcpServers: Required":** the Copilot CLI's own configuration file is empty or invalid. The Agents window reads it as well as the VS Code configuration. Delete it, then restart VS Code:

  Windows (PowerShell):

  ```powershell
  Remove-Item "$HOME\.copilot\mcp-config.json"
  ```

  macOS / Linux:

  ```sh
  rm ~/.copilot/mcp-config.json
  ```

- **Copilot lists the tools but never calls them:** it's probably reading them from the code. Ask again as in step 3, telling it to use the server and not the code.
- **It works in MCP Inspector but not in VS Code:** the server is fine, so check the URL and header in the VS Code configuration.

### Claude Code

- **Claude Code shows `trading` as failed, with a 401:** the token doesn't match. Stop the server, run `claude mcp remove trading`, add it again with the right token, then start the server again.
- **Claude Code doesn't list `trading` in `/mcp` at all:** either the session started before you added the server, so start a new one, or you ran `claude mcp add` from a different folder. Run `claude mcp list` from the repository root to check, and add it again from there if it's missing.
- **Still stuck installing Claude Code:** if you have the Claude Code extension in VS Code, ask it to add the `claude` install folder to your PATH.

## Other MCP hosts

The server isn't tied to VS Code. Any MCP host that can connect to a remote (HTTP) server and send an `Authorization` header can use it, with the same URL, `http://localhost:5080/mcp`, and the same token. See your host's documentation for how to add a server.

Next: optional [Azure deployment](04-azure-deployment.md) or [using a local model](05-local-llm.md).
