namespace TradingMcp;

// A setup problem the person running the server can fix, such as a missing user-secret.
// Program prints the message on its own, without a stack trace.
internal sealed class ServerConfigurationException(string message) : Exception(message);
