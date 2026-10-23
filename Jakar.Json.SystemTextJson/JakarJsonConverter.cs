// Jakar.Json.SystemTextJson
// 10/17/2026

using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;



namespace Jakar.Json.SystemTextJson;


/// <summary>
///     Reads and writes <typeparamref name="T"/> (a <c>[JsonModel]</c> type, or an array, list or collection interface of one) with Jakar.Json inside System.Text.Json. Add it to
///     <see cref="JsonSerializerOptions.Converters"/> for one type, or use <see cref="JakarJsonConverterFactory"/> for every model.
/// </summary>
/// <remarks>
///     Writing: Jakar.Json writes the value into a pooled buffer, which is copied into the <see cref="Utf8JsonWriter"/> verbatim (Jakar.Json's own spacing and indentation settings apply).
///     Reading: the value's raw UTF-8 is taken from the reader (System.Text.Json buffers a whole value before calling a converter) and parsed by Jakar.Json; the value is scanned once more
///     by System.Text.Json to find its end. The bridge costs one copy each way; the model's reading and writing still run on Jakar.Json.
/// </remarks>
public sealed class JakarJsonConverter<T> : JsonConverter<T>
{
    private readonly JsonTypeCodec __codec;


    /// <exception cref="NotSupportedException"> Jakar.Json can't serialize <typeparamref name="T"/>. </exception>
    public JakarJsonConverter() => __codec = JsonModels<T>.RequiredCodec;


    public override T? Read( ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options )
    {
        if ( reader.TokenType == JsonTokenType.Null )
        {
            return default(T) is null
                       ? default
                       : throw new JsonException($"Cannot convert JSON null to {typeof(T)}");
        }

        using JsonDocument document = JsonDocument.ParseValue(ref reader);

        // Hosts built on System.Text.Json catch JsonException (ASP.NET Core's 400s, message retries): Jakar.Json's own exception is translated, with its path and position.
        try { return (T?)__codec.Read(JsonMarshal.GetRawUtf8Value(document.RootElement)); }
        catch ( JsonReadException exception ) { throw new JsonException(exception.Message, exception.Path, exception.LineNumber, exception.BytePositionInLine, exception); }
    }

    public override void Write( Utf8JsonWriter writer, T value, JsonSerializerOptions options )
    {
        if ( value is null )
        {
            writer.WriteNullValue();
            return;
        }

        using PooledBufferWriter buffer = __codec.WriteToBuffer(value);
        writer.WriteRawValue(buffer.WrittenSpan, skipInputValidation: true);
    }
}
