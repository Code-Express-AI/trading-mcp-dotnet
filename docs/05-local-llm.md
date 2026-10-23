# 5. Optional: use a local model

You can run the same MCP server with a model on your own machine instead of a cloud model. Nothing changes on the server: VS Code still connects to it in the same way, and only the model behind the chat is different. This guide uses Ollama.

A local model also means the trading data never leaves your machine, which matters when the data is real.

## Before you start

- Complete guides [1](01-docker-database.md), [2](02-run-server-and-inspector.md) and [3](03-copilot-or-claude-code.md), and keep the server running.
- Install [Ollama](https://ollama.com/).

## Set up Ollama in VS Code

1. Install the [Ollama extension for VS Code](https://marketplace.visualstudio.com/items?itemName=Ollama.ollama).
2. Download a model that supports tool calling, for example:
   ```sh
   ollama pull qwen3.6
   ```
   Choose one that fits your machine's memory. Larger models need more memory, but follow multi-step questions more reliably.
3. In VS Code Chat, select the Ollama model in the model picker.
4. Switch to **Agent** mode and check that the `trading` tools are selected in the tools picker.
5. Ask a question, for example: "What were the five biggest movers on the last trading day? Which ones were losers?"

Start with simple questions. Smaller models handle one or two tool calls well, but may lose track of questions that need several in a row, such as the Thompson example in guide 3.

If the tools don't appear, check the model is selected in Agent mode, then test the server with [MCP Inspector](02-run-server-and-inspector.md#2-test-it-with-mcp-inspector). Support for local models and tool calling varies between VS Code, extension and model versions; see the [Ollama VS Code guide](https://docs.ollama.com/integrations/vscode) for the current requirements.
