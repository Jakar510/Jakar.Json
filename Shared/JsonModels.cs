// Jakar.Json (shared source, compiled into each integration package)
// 10/17/2026

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;



namespace Jakar.Json.Integration;


/// <summary>
///     Jakar.Json for a <typeparamref name="T"/> known only as a type parameter (no <see cref="IJsonSerializable{TSelf}"/> constraint): the <see cref="JsonTypeCodec"/> that
///     <see cref="JsonCodecRegistry"/> resolves for it, looked up once. Covers <c>[JsonModel]</c> types and arrays, lists and collection interfaces of them.
/// </summary>
internal static class JsonModels<T>
{
    public static readonly JsonTypeCodec? Codec = JsonCodecRegistry.TryGet(typeof(T), out JsonTypeCodec? codec)
                                                      ? codec
                                                      : null;


    public static bool IsSupported => Codec is not null;

    public static JsonTypeCodec RequiredCodec => Codec ?? throw Unsupported();


    public static void Write( T? value, IBufferWriter<byte> output ) => RequiredCodec.Write(value, ref output);

    public static byte[] ToUtf8( T? value )
    {
        using PooledBufferWriter buffer = RequiredCodec.WriteToBuffer(value);
        return buffer.ToArray();
    }

    public static async ValueTask WriteAsync( T? value, Stream stream, CancellationToken token = default )
    {
        using PooledBufferWriter buffer = RequiredCodec.WriteToBuffer(value);
        await stream.WriteAsync(buffer.WrittenMemory, token).ConfigureAwait(false);
    }

    public static T? Read( ReadOnlySpan<byte> utf8Json ) => (T?)RequiredCodec.Read(utf8Json);

    public static T? Read( in ReadOnlySequence<byte> utf8Json ) => (T?)RequiredCodec.Read(utf8Json);

    public static async ValueTask<T?> ReadAsync( Stream utf8Json, CancellationToken token = default ) => (T?)await RequiredCodec.ReadAsync(utf8Json, token: token).ConfigureAwait(false);


    public static NotSupportedException Unsupported() => JsonModels.Unsupported(typeof(T));
}



/// <summary> Jakar.Json for a <see cref="Type"/> known only at run time. </summary>
internal static class JsonModels
{
    public static bool IsSupported( Type type ) => JsonCodecRegistry.TryGet(type, out _);

    public static JsonTypeCodec RequiredCodec( Type type ) => JsonCodecRegistry.TryGet(type, out JsonTypeCodec? codec)
                                                                  ? codec
                                                                  : throw Unsupported(type);

    /// <summary> Writes <paramref name="value"/> into a new <see cref="PooledBufferWriter"/>, which the caller disposes. </summary>
    public static PooledBufferWriter WriteToBuffer( this JsonTypeCodec codec, object? value )
    {
        PooledBufferWriter buffer = new();

        try
        {
            codec.Write(value, ref buffer);
            return buffer;
        }
        catch
        {
            buffer.Dispose();
            throw;
        }
    }

    public static NotSupportedException Unsupported( Type type ) => new($"{type} isn't a [JsonModel] type or an array, list or collection interface of one, so Jakar.Json can't serialize it");
}
