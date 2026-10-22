// Jakar.Json.RestSharp
// 10/17/2026

using System.Text;
using RestSharp;
using RestSharp.Serializers;
using RestSharp.Serializers.Json;



namespace Jakar.Json.RestSharp;


/// <summary>
///     RestSharp's serializer on Jakar.Json: <c>new RestClient(options, configureSerialization: s =&gt; s.UseJakarJson())</c>. Bodies of <c>[JsonModel]</c> types (and arrays, lists and
///     collection interfaces of them) are written and read by Jakar.Json (responses parsed from their raw bytes); every other type goes to <see cref="Fallback"/>.
/// </summary>
/// <param name="fallback"> Serializes the types Jakar.Json can't, and decides which content types are JSON. Defaults to RestSharp's <see cref="SystemTextJsonSerializer"/>. </param>
public sealed class JakarJsonRestSerializer( IRestSerializer? fallback = null ) : IRestSerializer, ISerializer, IDeserializer
{
    public IRestSerializer Fallback { get; } = fallback ?? new SystemTextJsonSerializer();


    public ISerializer          Serializer           => this;
    public IDeserializer        Deserializer         => this;
    public string[]             AcceptedContentTypes => Fallback.AcceptedContentTypes;
    public SupportsContentType  SupportsContentType  => Fallback.SupportsContentType;
    public DataFormat           DataFormat           => DataFormat.Json;
    public ContentType          ContentType          { get; set; } = ContentType.Json;


    public string? Serialize( Parameter parameter ) => Serialize(parameter.Value);

    public string? Serialize( object? obj )
    {
        if ( obj is null ) { return null; }

        if ( !JsonCodecRegistry.TryGet(obj.GetType(), out JsonTypeCodec? codec) ) { return Fallback.Serializer.Serialize(obj); }

        using PooledBufferWriter buffer = codec.WriteToBuffer(obj);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public T? Deserialize<T>( RestResponse response )
    {
        ArgumentNullException.ThrowIfNull(response);
        if ( !JsonModels<T>.IsSupported ) { return Fallback.Deserializer.Deserialize<T>(response); }

        if ( response.RawBytes is { Length: > 0 } raw ) { return JsonModels<T>.Read(raw); }

        return string.IsNullOrEmpty(response.Content)
                   ? default
                   : JsonModels<T>.Read(Encoding.UTF8.GetBytes(response.Content));
    }
}



public static class JakarJsonRestSharpExtensions
{
    /// <summary> Uses <see cref="JakarJsonRestSerializer"/> (with RestSharp's System.Text.Json serializer as the fallback) for JSON. </summary>
    public static SerializerConfig UseJakarJson( this SerializerConfig config, IRestSerializer? fallback = null )
    {
        ArgumentNullException.ThrowIfNull(config);
        return config.UseSerializer(() => new JakarJsonRestSerializer(fallback));
    }
}
