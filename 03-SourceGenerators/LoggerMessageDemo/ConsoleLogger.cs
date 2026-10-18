using Microsoft.Extensions.Logging;

namespace LoggerMessageDemo;

// Minimal ILogger that writes to the console. Production code would use
// a real provider (Console, Debug, Serilog, ...); this keeps the demo
// dependency-free while the [LoggerMessage] methods stay identical.
public sealed class ConsoleLogger : ILogger
{
    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
        NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel,
                            EventId eventId,
                            TState state,
                            Exception? exception,
                            Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        Console.WriteLine($"[{logLevel}] {formatter(state, exception)}");
    }
}

public sealed class ConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new ConsoleLogger();

    public void Dispose() { }
}
