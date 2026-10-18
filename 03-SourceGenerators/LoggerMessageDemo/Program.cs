using LoggerMessageDemo;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder =>
    builder.AddProvider(new ConsoleLoggerProvider())
           .SetMinimumLevel(LogLevel.Trace));

ILogger logger = loggerFactory.CreateLogger("Demo");

// These two calls look ordinary, but OrderProcessing and StockLow are
// generated at compile time by the [LoggerMessage] source generator —
// open the build output and you'll find the emitted code.
AppLogging.OrderProcessing(logger, Guid.NewGuid(), "Acme Corp");
AppLogging.StockLow(logger, 42, 3);

Console.WriteLine();
Console.WriteLine("Both methods above are source-generated: strongly typed,");
Console.WriteLine("no boxing, no params-array allocations at the call site.");
