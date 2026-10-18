using Microsoft.Extensions.Logging;

namespace LoggerMessageDemo;

// The [LoggerMessage] attribute triggers the logging source generator that
// ships with the .NET SDK — no extra NuGet package needed. At compile time
// it emits strongly-typed methods: no boxing of value types, no
// params object[] allocation, and no runtime parsing of the template.
// A mismatch between {placeholders} and parameters is a compile error.
internal static partial class AppLogging
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Processing order {OrderId} for {Customer}")]
    internal static partial void OrderProcessing(ILogger logger, Guid orderId, string customer);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Stock low for product {ProductId}: {Stock} remaining")]
    internal static partial void StockLow(ILogger logger, int productId, int stock);
}
