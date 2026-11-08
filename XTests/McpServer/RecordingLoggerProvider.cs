using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace XTests.McpServer;

// Records every log entry, so the pipeline tests can check what a rejected request logged.
internal sealed class RecordingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<(LogLevel Level, string Message)> Entries { get; } = new();

    public ILogger CreateLogger(string categoryName) => new RecordingLogger(Entries);

    public void Dispose()
    {
    }

    private sealed class RecordingLogger(ConcurrentQueue<(LogLevel Level, string Message)> entries) : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            entries.Enqueue((logLevel, formatter(state, exception)));

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
