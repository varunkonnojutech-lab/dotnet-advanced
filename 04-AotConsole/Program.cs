using System.Text.Json;
using AotDemo;

var orders = new List<ShopOrder>
{
    new(Guid.NewGuid(), "Acme Corp", 199.99m, DateTimeOffset.UtcNow),
    new(Guid.NewGuid(), "Globex", 42.50m, DateTimeOffset.UtcNow),
};

// No reflection anywhere: the serializer is the generated JsonTypeInfo.
string json = JsonSerializer.Serialize(orders, OrderJsonContext.Default.ListShopOrder);
Console.WriteLine(json);

var roundTripped = JsonSerializer.Deserialize(json, OrderJsonContext.Default.ListShopOrder);
Console.WriteLine();
Console.WriteLine($"Deserialized {roundTripped?.Count ?? 0} orders; first customer: {roundTripped?[0].Customer}");
