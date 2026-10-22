// Jakar.Json.Http
// 10/17/2026

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;



namespace Jakar.Json.Http;


/// <summary>
///     HTTP content holding a <c>[JsonModel]</c> value (or an array, list or collection interface of one), written as Jakar.Json UTF-8 when the request is sent. Like
///     <c>System.Net.Http.Json.JsonContent</c>, the length isn't known in advance, so the body is sent chunked (or buffered by the handler).
/// </summary>
public sealed class JakarJsonContent : HttpContent
{
    /// <summary> <c>application/json; charset=utf-8</c>. </summary>
    public static MediaTypeHeaderValue DefaultMediaType => new("application/json") { CharSet = "utf-8" };


    private readonly JsonTypeCodec __codec;


    private JakarJsonContent( object? value, Type type, JsonTypeCodec codec, MediaTypeHeaderValue? mediaType )
    {
        Value               = value;
        ObjectType          = type;
        __codec             = codec;
        Headers.ContentType = mediaType ?? DefaultMediaType;
    }


    /// <summary> The value that will be written. </summary>
    public object? Value { get; }

    /// <summary> The type it's written as. </summary>
    public Type ObjectType { get; }


    /// <exception cref="NotSupportedException"> Jakar.Json can't serialize <typeparamref name="T"/>. </exception>
    public static JakarJsonContent Create<T>( T value, MediaTypeHeaderValue? mediaType = null ) => new(value, typeof(T), JsonModels<T>.RequiredCodec, mediaType);


    protected override Task SerializeToStreamAsync( Stream stream, TransportContext? context ) => SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override async Task SerializeToStreamAsync( Stream stream, TransportContext? context, CancellationToken cancellationToken )
    {
        using PooledBufferWriter buffer = __codec.WriteToBuffer(Value);
        await stream.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }

    protected override void SerializeToStream( Stream stream, TransportContext? context, CancellationToken cancellationToken )
    {
        using PooledBufferWriter buffer = __codec.WriteToBuffer(Value);
        stream.Write(buffer.WrittenSpan);
    }

    protected override bool TryComputeLength( out long length )
    {
        length = 0;
        return false;
    }
}
