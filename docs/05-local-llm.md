# 5. Optional: use a local LLM from VS Code

You can use the local MCP server with a local model instead of a hosted model. This guide uses Ollama's VS Code integration; the MCP server and database setup are the same as in the local guides.

## Prerequisites

- Complete [the database setup](01-docker-database.md).
- Complete [the VS Code MCP configuration](03-vscode-copilot.md) and keep the server running.
- Install [Ollama](https://ollama.com/) and [VS Code](https://code.visualstudio.com/).

## Set up Ollama in VS Code

1. Install the [Ollama extension for VS Code](https://marketplace.visualstudio.com/items?itemName=Ollama.ollama).
2. Download a model supported by Ollama, for example:
   ```sh
   ollama pull qwen3.6
   ```
   Choose a model that fits your machine and supports tool calling. Local model requirements vary; a larger context or model may require more memory.
3. Open VS Code Chat and select the Ollama model from the model picker.
4. Start a chat in **Agent** mode and check that the `trading` MCP tools are enabled.
5. Ask a question such as: “What were the five biggest daily movers on the latest trading day? Which ones were losers?”

The MCP server remains a separate .NET process; Ollama supplies the model, while VS Code connects that model to the MCP tools. Local model and tool-calling support can vary by VS Code, extension, and model version. If tools are unavailable, confirm the model is selected in Agent mode and test the MCP connection with [Inspector](02-run-server-and-inspector.md).

See the [Ollama VS Code guide](https://docs.ollama.com/integrations/vscode) for current version requirements and model setup.
