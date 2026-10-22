// Jakar.Json.EasyNetQ
// 10/17/2026

using EasyNetQ;
using EasyNetQ.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;



namespace Jakar.Json.EasyNetQ;


/// <summary>
///     EasyNetQ's message serializer on Jakar.Json: <c>services.AddEasyNetQ(...).UseJakarJson()</c>. Messages of <c>[JsonModel]</c> types (and arrays, lists and collection interfaces of
///     them) are written into a pooled buffer that EasyNetQ owns (and returns after publishing), and read straight from the delivery; every other type goes to <see cref="Fallback"/>.
/// </summary>
/// <param name="fallback"> Serializes the types Jakar.Json can't. </param>
public sealed class JakarJsonEasyNetQSerializer( ISerializer fallback ) : ISerializer
{
    public ISerializer Fallback { get; } = fallback ?? throw new ArgumentNullException(nameof(fallback));


    public IMemoryOwner<byte> MessageToBytes( Type messageType, object message )
    {
        if ( !JsonCodecRegistry.TryGet(messageType, out JsonTypeCodec? codec) ) { return Fallback.MessageToBytes(messageType, message); }

        PooledBufferWriter buffer = new();

        try { codec.Write(message, ref buffer); }
        catch
        {
            buffer.Dispose();
            throw;
        }

        return buffer;
    }

    public object BytesToMessage( Type messageType, in ReadOnlyMemory<byte> bytes ) => JsonCodecRegistry.TryGet(messageType, out JsonTypeCodec? codec)
                                                                                        ? codec.Read(bytes.Span)!
                                                                                        : Fallback.BytesToMessage(messageType, bytes);
}



public static class JakarJsonEasyNetQExtensions
{
    /// <summary> Serializes messages with Jakar.Json; other types use EasyNetQ's System.Text.Json serializer. </summary>
    [RequiresUnreferencedCode("The default fallback is EasyNetQ's reflection-based System.Text.Json serializer; pass a fallback built on a JsonSerializerContext when trimming")]
    [RequiresDynamicCode("The default fallback is EasyNetQ's reflection-based System.Text.Json serializer; pass a fallback built on a JsonSerializerContext when trimming")]
    public static IEasyNetQBuilder UseJakarJson( this IEasyNetQBuilder builder ) => builder.UseJakarJson(new SystemTextJsonSerializer());

    /// <summary> Serializes messages with Jakar.Json; other types use <paramref name="fallback"/>. </summary>
    public static IEasyNetQBuilder UseJakarJson( this IEasyNetQBuilder builder, ISerializer fallback )
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.Replace(ServiceDescriptor.Singleton<ISerializer>(new JakarJsonEasyNetQSerializer(fallback)));
        return builder;
    }
}
