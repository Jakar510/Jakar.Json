// Jakar.Json.Cosmos
// 10/17/2026

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos;



namespace Jakar.Json.Cosmos;


/// <summary>
///     The Cosmos DB serializer on Jakar.Json: <c>new CosmosClientOptions { Serializer = new JakarJsonCosmosSerializer() }</c>. Items of <c>[JsonModel]</c> types (and arrays, lists and
///     collection interfaces of them) are read and written by Jakar.Json; every other type goes to <see cref="Fallback"/>, which also names members for LINQ queries.
/// </summary>
/// <remarks>
///     LINQ: Cosmos builds query paths from <see cref="SerializeMemberName"/>, answered by the fallback. Give the fallback the naming your models use (camelCase by default here, matching
///     <c>[JsonModel(Naming = JsonNaming.CamelCase)]</c>) so queries name the same properties Jakar.Json writes.
/// </remarks>
/// <param name="fallback"> Serializes other types and names LINQ members. Defaults to <see cref="SystemTextJsonCosmosSerializer"/> with camelCase. </param>
public sealed class JakarJsonCosmosSerializer( CosmosLinqSerializer? fallback = null ) : CosmosLinqSerializer
{
    public CosmosLinqSerializer Fallback { get; } = fallback ?? new SystemTextJsonCosmosSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));


    public override T FromStream<T>( Stream stream )
    {
        ArgumentNullException.ThrowIfNull(stream);
        if ( typeof(Stream).IsAssignableFrom(typeof(T)) ) { return (T)(object)stream; } // Cosmos' convention: streams pass through
        if ( !JsonModels<T>.IsSupported ) { return Fallback.FromStream<T>(stream); }

        using ( stream )
        {
            if ( stream is MemoryStream memory && memory.TryGetBuffer(out ArraySegment<byte> segment) ) { return (T)JsonModels<T>.Read(segment.AsSpan((int)memory.Position))!; }

            using PooledBufferWriter buffer = new PooledBufferWriter().ReadFrom(stream);

            return (T)JsonModels<T>.Read(buffer.WrittenSpan)!;
        }
    }

    public override Stream ToStream<T>( T input )
    {
        if ( !JsonModels<T>.IsSupported ) { return Fallback.ToStream(input); }

        using PooledBufferWriter buffer = new();
        JsonModels<T>.Write(input, buffer);
        return new MemoryStream(buffer.ToArray(), writable: false);
    }

    public override string SerializeMemberName( MemberInfo memberInfo ) => Fallback.SerializeMemberName(memberInfo);
}



/// <summary> A System.Text.Json <see cref="CosmosLinqSerializer"/>: the default fallback of <see cref="JakarJsonCosmosSerializer"/> (the SDK's own is internal). </summary>
public sealed class SystemTextJsonCosmosSerializer( JsonSerializerOptions options ) : CosmosLinqSerializer
{
    public JsonSerializerOptions Options { get; } = options ?? throw new ArgumentNullException(nameof(options));


    public override T FromStream<T>( Stream stream )
    {
        ArgumentNullException.ThrowIfNull(stream);
        if ( typeof(Stream).IsAssignableFrom(typeof(T)) ) { return (T)(object)stream; }

        using ( stream ) { return JsonSerializer.Deserialize<T>(stream, Options)!; }
    }

    public override Stream ToStream<T>( T input )
    {
        MemoryStream stream = new();
        JsonSerializer.Serialize(stream, input, Options);
        stream.Position = 0;
        return stream;
    }

    public override string SerializeMemberName( MemberInfo memberInfo )
    {
        ArgumentNullException.ThrowIfNull(memberInfo);
        if ( memberInfo.GetCustomAttribute<JsonPropertyNameAttribute>() is { } named ) { return named.Name; }

        return Options.PropertyNamingPolicy?.ConvertName(memberInfo.Name) ?? memberInfo.Name;
    }
}
