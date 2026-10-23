// Jakar.Json.Rebus
// 10/17/2026

using Rebus.Config;
using Rebus.Messages;
using Rebus.Serialization;



namespace Jakar.Json.Rebus;


/// <summary>
///     Rebus' serializer on Jakar.Json: message bodies of <c>[JsonModel]</c> types (and arrays, lists and collection interfaces of them) are written and read by Jakar.Json, straight to and
///     from the transport message's bytes; every other body goes to <see cref="Fallback"/> (the serializer Rebus would otherwise use). The type and content-type headers are the ones
///     Rebus' JSON serializer writes, so endpoints without Jakar.Json still read the messages.
/// </summary>
/// <param name="fallback"> Serializes the bodies Jakar.Json can't. </param>
/// <param name="types"> Names the message types in the <see cref="Headers.Type"/> header, and resolves them when reading. </param>
public sealed class JakarJsonRebusSerializer( ISerializer fallback, IMessageTypeNameConvention types ) : ISerializer
{
    public const string CONTENT_TYPE = "application/json;charset=utf-8";

    public ISerializer                Fallback { get; } = fallback ?? throw new ArgumentNullException(nameof(fallback));
    public IMessageTypeNameConvention Types    { get; } = types    ?? throw new ArgumentNullException(nameof(types));


    public Task<TransportMessage> Serialize( Message message )
    {
        ArgumentNullException.ThrowIfNull(message);
        if ( message.Body is not { } body || !JsonCodecRegistry.TryGet(body.GetType(), out JsonTypeCodec? codec) ) { return Fallback.Serialize(message); }

        Dictionary<string, string> headers = new(message.Headers)
                                             {
                                                 [Headers.Type]        = Types.GetTypeName(body.GetType()),
                                                 [Headers.ContentType] = CONTENT_TYPE
                                             };

        using PooledBufferWriter buffer = codec.WriteToBuffer(body);
        return Task.FromResult(new TransportMessage(headers, buffer.ToArray()));
    }

    public Task<Message> Deserialize( TransportMessage transportMessage )
    {
        ArgumentNullException.ThrowIfNull(transportMessage);
        if ( !TryGetCodec(transportMessage.Headers, out JsonTypeCodec? codec) ) { return Fallback.Deserialize(transportMessage); }

        object body = codec.Read(transportMessage.Body) ?? throw new FormatException($"The message body is JSON null, not a {transportMessage.Headers[Headers.Type]}");
        return Task.FromResult(new Message(new Dictionary<string, string>(transportMessage.Headers), body));
    }


    /// <summary> A codec for UTF-8 JSON (no content encoding) of a type Jakar.Json handles; anything else is the fallback's to read. </summary>
    private bool TryGetCodec( Dictionary<string, string> headers, [NotNullWhen(true)] out JsonTypeCodec? codec )
    {
        codec = null;

        if ( !headers.TryGetValue(Headers.Type, out string? name) || headers.ContainsKey(Headers.ContentEncoding) ) { return false; }

        if ( headers.TryGetValue(Headers.ContentType, out string? contentType) && !contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) ) { return false; }

        return Types.GetType(name) is { } type && JsonCodecRegistry.TryGet(type, out codec);
    }
}



public static class JakarJsonRebusExtensions
{
    /// <summary>
    ///     Writes and reads <c>[JsonModel]</c> message bodies with Jakar.Json; the serializer configured before this one (Rebus' default when none is) handles every other body and Rebus'
    ///     headers: <c>.Serialization(s =&gt; s.UseJakarJson())</c>.
    /// </summary>
    public static void UseJakarJson( this StandardConfigurer<ISerializer> configurer )
    {
        ArgumentNullException.ThrowIfNull(configurer);
        configurer.Decorate(static c => new JakarJsonRebusSerializer(c.Get<ISerializer>(), c.Get<IMessageTypeNameConvention>()));
    }
}
