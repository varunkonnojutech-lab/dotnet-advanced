using System.Text.Json.Serialization;

namespace AotDemo;

public sealed record ShopOrder(Guid Id, string Customer, decimal Total, DateTimeOffset PlacedAt);

// Native AOT forbids runtime reflection, so System.Text.Json cannot discover
// the shape of ShopOrder at runtime. This partial class triggers the JSON
// source generator, which emits the serialization code at compile time.
// The generated metadata is reached via OrderJsonContext.Default.
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ShopOrder))]
[JsonSerializable(typeof(List<ShopOrder>))]
internal partial class OrderJsonContext : JsonSerializerContext
{
}
