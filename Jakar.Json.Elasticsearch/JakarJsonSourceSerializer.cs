// Jakar.Json.Elasticsearch
// 10/17/2026

using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Serialization;
using Elastic.Transport;



namespace Jakar.Json.Elasticsearch;


/// <summary>
///     The Elasticsearch client's source serializer on Jakar.Json: <c>new ElasticsearchClientSettings(pool, JakarJsonSourceSerializer.Factory())</c>. Documents (<c>_source</c>) of
///     <c>[JsonModel]</c> types (and arrays, lists and collection interfaces of them) are written and read by Jakar.Json; every other type goes to <see cref="Fallback"/>.
/// </summary>
/// <param name="fallback"> Serializes other types (usually the client's <see cref="DefaultSourceSerializer"/>). </param>
public sealed class JakarJsonSourceSerializer( Serializer fallback ) : Serializer
{
    public Serializer Fallback { get; } = fallback ?? throw new ArgumentNullException(nameof(fallback));


    /// <summary> A <see cref="ElasticsearchClientSettings.SourceSerializerFactory"/> using Jakar.Json, with the client's <see cref="DefaultSourceSerializer"/> (configured by <paramref name="configureFallback"/>) for other types. </summary>
    public static ElasticsearchClientSettings.SourceSerializerFactory Factory( Action<JsonSerializerOptions>? configureFallback = null ) => ( _, settings ) => new JakarJsonSourceSerializer(new DefaultSourceSerializer(settings, configureFallback));


    public override object? Deserialize( Type type, Stream stream )
    {
        if ( !JsonCodecRegistry.TryGet(type, out JsonTypeCodec? codec) ) { return Fallback.Deserialize(type, stream); }

        using PooledBufferWriter buffer = new PooledBufferWriter().ReadFrom(stream);
        return codec.Read(buffer.WrittenSpan);
    }

    public override T Deserialize<T>( Stream stream ) => JsonModels<T>.IsSupported
                                                             ? (T)Deserialize(typeof(T), stream)!
                                                             : Fallback.Deserialize<T>(stream);

    public override async ValueTask<object?> DeserializeAsync( Type type, Stream stream, CancellationToken cancellationToken = default )
    {
        if ( !JsonCodecRegistry.TryGet(type, out JsonTypeCodec? codec) ) { return await Fallback.DeserializeAsync(type, stream, cancellationToken).ConfigureAwait(false); }

        return await codec.ReadAsync(stream, token: cancellationToken).ConfigureAwait(false);
    }

    public override async ValueTask<T> DeserializeAsync<T>( Stream stream, CancellationToken cancellationToken = default ) => JsonModels<T>.IsSupported
                                                                                                                                  ? (T)( await DeserializeAsync(typeof(T), stream, cancellationToken).ConfigureAwait(false) )!
                                                                                                                                  : await Fallback.DeserializeAsync<T>(stream, cancellationToken).ConfigureAwait(false);

    public override void Serialize( object? data, Type type, Stream stream, SerializationFormatting formatting = SerializationFormatting.None, CancellationToken cancellationToken = default )
    {
        if ( !JsonCodecRegistry.TryGet(type, out JsonTypeCodec? codec) )
        {
            Fallback.Serialize(data, type, stream, formatting, cancellationToken);
            return;
        }

        using PooledBufferWriter buffer = new();
        codec.Write(data, buffer);
        stream.Write(buffer.WrittenSpan);
    }

    public override void Serialize<T>( T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.None )
    {
        if ( JsonModels<T>.IsSupported ) { Serialize(data, typeof(T), stream, formatting); }
        else { Fallback.Serialize(data, stream, formatting); }
    }

    public override async Task SerializeAsync( object? data, Type type, Stream stream, SerializationFormatting formatting = SerializationFormatting.None, CancellationToken cancellationToken = default )
    {
        if ( !JsonCodecRegistry.TryGet(type, out JsonTypeCodec? codec) )
        {
            await Fallback.SerializeAsync(data, type, stream, formatting, cancellationToken).ConfigureAwait(false);
            return;
        }

        using PooledBufferWriter buffer = new();
        codec.Write(data, buffer);
        await stream.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }

    public override Task SerializeAsync<T>( T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.None, CancellationToken cancellationToken = default ) => JsonModels<T>.IsSupported
                                                                                                                                                                                      ? SerializeAsync(data, typeof(T), stream, formatting, cancellationToken)
                                                                                                                                                                                      : Fallback.SerializeAsync(data, stream, formatting, cancellationToken);
}
