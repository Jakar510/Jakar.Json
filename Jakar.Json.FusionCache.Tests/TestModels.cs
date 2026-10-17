// Jakar.Json.FusionCache.Tests
// 10/17/2026

using Jakar.Extensions;



namespace Jakar.Json.FusionCache.Tests;


[JsonModel]
public sealed partial record CachedOrder
{
    public int              Id       { get; init; }
    public string           Customer { get; init; } = "";
    public decimal          Total    { get; init; }
    public List<CachedLine> Lines    { get; init; } = [];

    public bool Equals( CachedOrder? other ) => other is not null && Id == other.Id && Customer == other.Customer && Total == other.Total && Lines.SequenceEqual(other.Lines);
    public override int GetHashCode() => HashCode.Combine(Id, Customer, Total);
}



[JsonModel]
public sealed partial record CachedLine
{
    public string  Sku      { get; init; } = "";
    public int     Quantity { get; init; }
    public decimal Price    { get; init; }
}



[JsonModel]
public partial struct CachedPoint
{
    public int X { get; set; }
    public int Y { get; set; }
}



/// <summary> Not a [JsonModel]: needs the fallback serializer. </summary>
public sealed record PlainDto( string Name );



public enum ByteColor : byte { Red = 1, Blue = 200 }



public enum LongFlags : long { None = 0, High = 1L << 62 }
