// Jakar.Json.OpenSearch
// 10/17/2026

using OpenSearch.Client;
using OpenSearch.Net;



namespace Jakar.Json.OpenSearch;


/// <summary>
///     The OpenSearch client's source serializer on Jakar.Json: <c>new ConnectionSettings(pool, JakarJsonOpenSearchSerializer.Factory)</c>. Documents (<c>_source</c>) of
///     <c>[JsonModel]</c> types (and arrays, lists and collection interfaces of them) are written and read by Jakar.Json; every other type goes to <see cref="Fallback"/>.
/// </summary>
/// <param name="fallback"> Serializes other types: the client's built-in serializer. </param>
public sealed class JakarJsonOpenSearchSerializer( IOpenSearchSerializer fallback ) : IOpenSearchSerializer
{
    /// <summary> Jakar.Json, with the client's built-in serializer for other types. </summary>
    public static readonly ConnectionSettings.SourceSerializerFactory Factory = static ( builtIn, _ ) => new JakarJsonOpenSearchSerializer(builtIn);


    public IOpenSearchSerializer Fallback { get; } = fallback ?? throw new ArgumentNullException(nameof(fallback));


    public object? Deserialize( Type type, Stream stream )
    {
        if ( !JsonCodecRegistry.TryGet(type, out JsonTypeCodec? codec) ) { return Fallback.Deserialize(type, stream); }

        using PooledBufferWriter buffer = new PooledBufferWriter().ReadFrom(stream);
        return codec.Read(buffer.WrittenSpan);
    }

    public T Deserialize<T>( Stream stream ) => JsonModels<T>.IsSupported
                                                    ? (T)Deserialize(typeof(T), stream)!
                                                    : Fallback.Deserialize<T>(stream);

    public async Task<object?> DeserializeAsync( Type type, Stream stream, CancellationToken cancellationToken = default ) => JsonCodecRegistry.TryGet(type, out JsonTypeCodec? codec)
                                                                                                                                 ? await codec.ReadAsync(stream, token: cancellationToken).ConfigureAwait(false)
                                                                                                                                 : await Fallback.DeserializeAsync(type, stream, cancellationToken).ConfigureAwait(false);

    public async Task<T> DeserializeAsync<T>( Stream stream, CancellationToken cancellationToken = default ) => JsonModels<T>.IsSupported
                                                                                                                  ? (T)( await DeserializeAsync(typeof(T), stream, cancellationToken).ConfigureAwait(false) )!
                                                                                                                  : await Fallback.DeserializeAsync<T>(stream, cancellationToken).ConfigureAwait(false);

    public void Serialize<T>( T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.None )
    {
        if ( !JsonModels<T>.IsSupported )
        {
            Fallback.Serialize(data, stream, formatting);
            return;
        }

        using PooledBufferWriter buffer = new();
        JsonModels<T>.Write(data, buffer);
        stream.Write(buffer.WrittenSpan);
    }

    public async Task SerializeAsync<T>( T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.None, CancellationToken cancellationToken = default )
    {
        if ( !JsonModels<T>.IsSupported )
        {
            await Fallback.SerializeAsync(data, stream, formatting, cancellationToken).ConfigureAwait(false);
            return;
        }

        using PooledBufferWriter buffer = new();
        JsonModels<T>.Write(data, buffer);
        await stream.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }
}
