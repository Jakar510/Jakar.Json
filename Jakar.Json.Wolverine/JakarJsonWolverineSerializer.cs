// Jakar.Json.Wolverine
// 10/17/2026

using Wolverine;
using Wolverine.Runtime.Serialization;



namespace Jakar.Json.Wolverine;


/// <summary>
///     Wolverine's message serializer on Jakar.Json: <c>opts.UseJakarJson()</c>. Messages of <c>[JsonModel]</c> types (and arrays, lists and collection interfaces of them) are written and
///     read by Jakar.Json, straight from the envelope's body; every other type goes to <see cref="Fallback"/> (Wolverine's System.Text.Json serializer). The content type stays
///     <c>application/json</c>.
/// </summary>
/// <param name="fallback"> Serializes the types Jakar.Json can't. </param>
public sealed class JakarJsonWolverineSerializer( IMessageSerializer fallback ) : IMessageSerializer
{
    public IMessageSerializer Fallback    { get; } = fallback ?? throw new ArgumentNullException(nameof(fallback));
    public string             ContentType => Fallback.ContentType;


    public byte[] Write( Envelope envelope )
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if ( envelope.Message is not { } message || !JsonCodecRegistry.TryGet(message.GetType(), out JsonTypeCodec? codec) ) { return Fallback.Write(envelope); }

        using PooledBufferWriter buffer = codec.WriteToBuffer(message);
        return buffer.ToArray();
    }

    public byte[] WriteMessage( object message )
    {
        ArgumentNullException.ThrowIfNull(message);
        if ( !JsonCodecRegistry.TryGet(message.GetType(), out JsonTypeCodec? codec) ) { return Fallback.WriteMessage(message); }

        using PooledBufferWriter buffer = codec.WriteToBuffer(message);
        return buffer.ToArray();
    }

    public object ReadFromData( Type messageType, Envelope envelope )
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return JsonCodecRegistry.TryGet(messageType, out JsonTypeCodec? codec)
                   ? codec.Read(envelope.Body.Span)!
                   : Fallback.ReadFromData(messageType, envelope);
    }

    /// <summary> Without a message type there's nothing to choose a codec by: the fallback reads it. </summary>
    public object ReadFromData( byte[] data ) => Fallback.ReadFromData(data);
}



public static class JakarJsonWolverineExtensions
{
    /// <summary> Serializes messages with Jakar.Json; Wolverine's current default serializer handles every other type. </summary>
    public static WolverineOptions UseJakarJson( this WolverineOptions options )
    {
        ArgumentNullException.ThrowIfNull(options);
        if ( options.DefaultSerializer is not JakarJsonWolverineSerializer ) { options.DefaultSerializer = new JakarJsonWolverineSerializer(options.DefaultSerializer); }

        return options;
    }
}
