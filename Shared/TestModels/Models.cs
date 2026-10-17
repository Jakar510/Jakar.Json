// Jakar.Json integration tests (shared source, compiled into each test project)
// 10/17/2026

using Jakar.Extensions;
using Jakar.Json;



namespace Jakar.Json.Tests.Shared;


[JsonModel(Naming = JsonNaming.CamelCase)]
public sealed partial record Order
{
    public int        Id       { get; init; }
    public string     Customer { get; init; } = "";
    public decimal    Total    { get; init; }
    public List<Line> Lines    { get; init; } = [];

    public bool Equals( Order? other ) => other is not null && Id == other.Id && Customer == other.Customer && Total == other.Total && Lines.SequenceEqual(other.Lines);
    public override int GetHashCode() => HashCode.Combine(Id, Customer, Total);
}



[JsonModel(Naming = JsonNaming.CamelCase)]
public sealed partial record Line
{
    public string  Sku      { get; init; } = "";
    public int     Quantity { get; init; }
    public decimal Price    { get; init; }
}



[JsonModel(Naming = JsonNaming.CamelCase)]
public partial struct Point
{
    public int X { get; set; }
    public int Y { get; set; }
}



/// <summary> Not a [JsonModel]: Jakar.Json can't serialize it. </summary>
public sealed record PlainDto( string Name );



/// <summary> A plain (System.Text.Json) type holding [JsonModel] members, as hosts that serialize their own envelopes do. </summary>
public sealed class OrderBatch
{
    public string       Kind   { get; set; } = "";
    public Order?       Order  { get; set; }
    public List<Order>? Orders { get; set; }
}



public static class Samples
{
    /// <summary> Jakar.Json writes " : " between names and values; System.Text.Json writes ":". That tells the serializers apart in a payload. </summary>
    public const string JAKAR_SEPARATOR = "\" : ";

    public static Order Order( int id = 7 ) => new()
                                               {
                                                   Id       = id,
                                                   Customer = "Zoë",
                                                   Total    = 12.50m,
                                                   Lines    = [new Line { Sku = "A-1", Quantity = 2, Price = 6.25m }]
                                               };

    public static List<Order> Orders() => [Order(1), Order(2)];
}
