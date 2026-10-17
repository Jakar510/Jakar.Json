// Jakar.Json.DurableTask
// 10/17/2026

using System.Text;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Converters;



namespace Jakar.Json.DurableTask;


/// <summary>
///     The Durable Task data converter on Jakar.Json: orchestration and activity inputs, outputs and events of <c>[JsonModel]</c> types (and arrays, lists and collection interfaces of them)
///     are written and read by Jakar.Json; every other type goes to <see cref="Fallback"/>. Set it on the client and worker options (<c>options.DataConverter = new JakarJsonDataConverter()</c>).
/// </summary>
/// <param name="fallback"> Serializes the types Jakar.Json can't. Defaults to <see cref="JsonDataConverter.Default"/> (System.Text.Json). </param>
public sealed class JakarJsonDataConverter( DataConverter? fallback = null ) : DataConverter
{
    public DataConverter Fallback { get; } = fallback ?? JsonDataConverter.Default;


    public override string? Serialize( object? value )
    {
        if ( value is null || !JsonCodecRegistry.TryGet(value.GetType(), out JsonTypeCodec? codec) ) { return Fallback.Serialize(value); }

        using PooledBufferWriter buffer = new();
        codec.Write(value, buffer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public override object? Deserialize( string? data, Type targetType )
    {
        ArgumentNullException.ThrowIfNull(targetType);
        if ( !JsonCodecRegistry.TryGet(targetType, out JsonTypeCodec? codec) ) { return Fallback.Deserialize(data, targetType); }

        return string.IsNullOrEmpty(data)
                   ? null
                   : codec.Read(Encoding.UTF8.GetBytes(data));
    }
}
