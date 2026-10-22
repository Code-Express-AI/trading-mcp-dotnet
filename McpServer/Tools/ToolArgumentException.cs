using ModelContextProtocol;

namespace TradingMcp.Tools;

// A problem with the arguments that the model can fix. The message goes back to the model as it is,
// so write it for the model: say what's wrong and what to do instead.
public sealed class ToolArgumentException(string message) : McpException(message);
