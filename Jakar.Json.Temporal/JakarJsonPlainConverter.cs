// Jakar.Json.Temporal
// 10/17/2026

using System.Text.Json;
using Google.Protobuf;
using Temporalio.Api.Common.V1;
using Temporalio.Converters;



namespace Jakar.Json.Temporal;


/// <summary>
///     Temporal's <c>json/plain</c> encoding with <c>[JsonModel]</c> values (and arrays, lists and collection interfaces of them) written and read by Jakar.Json; every other value goes to
///     <see cref="Fallback"/> (Temporal's <see cref="JsonPlainConverter"/>). It takes the fallback's place in the payload converter, so the encoding name stays <c>json/plain</c> and workers
///     without Jakar.Json read the payloads.
/// </summary>
public sealed class JakarJsonPlainConverter( JsonPlainConverter fallback ) : IEncodingConverter
{
    private static readonly ByteString __encoding = ByteString.CopyFromUtf8("json/plain");


    public JsonPlainConverter Fallback { get; } = fallback ?? throw new ArgumentNullException(nameof(fallback));
    public string             Encoding => Fallback.Encoding;


    public bool TryToPayload( object? value, out Payload? payload )
    {
        if ( value is null || !JsonCodecRegistry.TryGet(value.GetType(), out JsonTypeCodec? codec) ) { return Fallback.TryToPayload(value, out payload); }

        using PooledBufferWriter buffer = codec.WriteToBuffer(value);

        payload = new Payload { Data = ByteString.CopyFrom(buffer.WrittenSpan) };
        payload.Metadata["encoding"] = __encoding;
        return true;
    }

    public object? ToValue( Payload payload, Type type )
    {
        ArgumentNullException.ThrowIfNull(payload);

        return JsonCodecRegistry.TryGet(type, out JsonTypeCodec? codec)
                   ? codec.Read(payload.Data.Span)
                   : Fallback.ToValue(payload, type);
    }
}



public static class JakarJsonTemporalExtensions
{
    /// <summary>
    ///     A payload converter with Temporal's default encodings, its <c>json/plain</c> converter replaced by <see cref="JakarJsonPlainConverter"/>: <c>new TemporalClientConnectOptions { DataConverter =
    ///     DataConverter.Default.WithJakarJson() }</c>.
    /// </summary>
    /// <param name="options"> System.Text.Json options for the values Jakar.Json doesn't handle (Temporal's defaults when <see langword="null"/>). </param>
    public static DefaultPayloadConverter CreatePayloadConverter( JsonSerializerOptions? options = null )
    {
        DefaultPayloadConverter defaults = options is null
                                               ? new DefaultPayloadConverter()
                                               : new DefaultPayloadConverter(options);

        return new DefaultPayloadConverter(defaults.EncodingConverters.Select(static c => c is JsonPlainConverter json ? new JakarJsonPlainConverter(json) : c).ToArray());
    }

    /// <summary> This data converter, with its payload converter replaced by <see cref="CreatePayloadConverter"/> (failure converter and codec kept). </summary>
    public static DataConverter WithJakarJson( this DataConverter converter, JsonSerializerOptions? options = null )
    {
        ArgumentNullException.ThrowIfNull(converter);
        return converter with { PayloadConverter = CreatePayloadConverter(options) };
    }
}
