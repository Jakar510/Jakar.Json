// Jakar.Json.Serilog
// 10/17/2026

using System.Text.Json;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;



namespace Jakar.Json.Serilog;


/// <summary>
///     Destructures <c>[JsonModel]</c> values (and arrays, lists and collection interfaces of them) logged with <c>{@Value}</c> from what Jakar.Json writes: the same property names, nulls
///     and ignored members as the JSON, with no reflection. Objects become <see cref="StructureValue"/>s tagged with the model's type name, arrays <see cref="SequenceValue"/>s.
/// </summary>
public sealed class JakarJsonDestructuringPolicy : IDestructuringPolicy
{
    public bool TryDestructure( object value, ILogEventPropertyValueFactory propertyValueFactory, [NotNullWhen(true)] out LogEventPropertyValue? result )
    {
        result = null;
        if ( value is null || !JsonCodecRegistry.TryGet(value.GetType(), out JsonTypeCodec? codec) ) { return false; }

        using PooledBufferWriter buffer = new();
        codec.Write(value, buffer);

        using JsonDocument document = JsonDocument.Parse(buffer.WrittenMemory);
        result = Convert(document.RootElement, value.GetType().Name);
        return true;
    }


    private static LogEventPropertyValue Convert( JsonElement element, string? typeTag ) => element.ValueKind switch
                                                                                             {
                                                                                                 JsonValueKind.Object => new StructureValue(element.EnumerateObject().Select(static p => new LogEventProperty(p.Name, Convert(p.Value, null))).ToArray(), typeTag),
                                                                                                 JsonValueKind.Array  => new SequenceValue(element.EnumerateArray().Select(static e => Convert(e, null)).ToArray()),
                                                                                                 JsonValueKind.String => new ScalarValue(element.GetString()),
                                                                                                 JsonValueKind.Number => new ScalarValue(Number(element)),
                                                                                                 JsonValueKind.True   => new ScalarValue(true),
                                                                                                 JsonValueKind.False  => new ScalarValue(false),
                                                                                                 _                    => new ScalarValue(null)
                                                                                             };

    /// <summary> An integer when the number is one, else a <see cref="decimal"/> when it fits (keeps the written digits), else a <see cref="double"/>. </summary>
    private static object Number( JsonElement element )
    {
        if ( element.TryGetInt64(out long integer) ) { return integer; }

        if ( element.TryGetDecimal(out decimal number) ) { return number; }

        return element.GetDouble();
    }
}



public static class JakarJsonSerilogExtensions
{
    /// <summary> Destructures <c>[JsonModel]</c> values with <see cref="JakarJsonDestructuringPolicy"/>: <c>new LoggerConfiguration().Destructure.UsingJakarJson()</c>. </summary>
    public static LoggerConfiguration UsingJakarJson( this LoggerDestructuringConfiguration configuration )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        return configuration.With(new JakarJsonDestructuringPolicy());
    }
}
