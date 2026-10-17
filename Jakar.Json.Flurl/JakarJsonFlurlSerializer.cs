// Jakar.Json.Flurl
// 10/17/2026

using System.Text;
using Flurl.Http;
using Flurl.Http.Configuration;



namespace Jakar.Json.Flurl;


/// <summary>
///     Flurl.Http's JSON serializer on Jakar.Json: <c>FlurlHttp.Clients.WithDefaults(b =&gt; b.Settings.JsonSerializer = new JakarJsonFlurlSerializer())</c>. Bodies of <c>[JsonModel]</c>
///     types (and arrays, lists and collection interfaces of them) are written and read by Jakar.Json, responses straight from the stream; every other type goes to <see cref="Fallback"/>.
/// </summary>
/// <param name="fallback"> Serializes the types Jakar.Json can't. Defaults to Flurl's <see cref="DefaultJsonSerializer"/> (System.Text.Json). </param>
public sealed class JakarJsonFlurlSerializer( ISerializer? fallback = null ) : ISerializer
{
    public ISerializer Fallback { get; } = fallback ?? new DefaultJsonSerializer();


    public string? Serialize( object? obj )
    {
        if ( obj is null || !JsonCodecRegistry.TryGet(obj.GetType(), out JsonTypeCodec? codec) ) { return Fallback.Serialize(obj); }

        using PooledBufferWriter buffer = new();
        codec.Write(obj, buffer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public T? Deserialize<T>( string? s )
    {
        if ( !JsonModels<T>.IsSupported ) { return Fallback.Deserialize<T>(s); }

        return string.IsNullOrEmpty(s)
                   ? default
                   : JsonModels<T>.Read(Encoding.UTF8.GetBytes(s));
    }

    public T? Deserialize<T>( Stream stream )
    {
        if ( !JsonModels<T>.IsSupported ) { return Fallback.Deserialize<T>(stream); }

        using PooledBufferWriter buffer = new PooledBufferWriter().ReadFrom(stream);
        return buffer.WrittenCount == 0
                   ? default
                   : JsonModels<T>.Read(buffer.WrittenSpan);
    }
}



public static class JakarJsonFlurlExtensions
{
    /// <summary> Uses <see cref="JakarJsonFlurlSerializer"/> (with the current serializer as its fallback) for this client or builder's settings. </summary>
    public static FlurlHttpSettings UseJakarJson( this FlurlHttpSettings settings )
    {
        ArgumentNullException.ThrowIfNull(settings);
        if ( settings.JsonSerializer is not JakarJsonFlurlSerializer ) { settings.JsonSerializer = new JakarJsonFlurlSerializer(settings.JsonSerializer); }

        return settings;
    }
}
