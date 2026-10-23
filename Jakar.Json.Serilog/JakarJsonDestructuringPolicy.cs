// Jakar.Json.Serilog
// 10/17/2026

using System.Numerics;
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

        using PooledBufferWriter buffer = codec.WriteToBuffer(value);

        using JsonTape tape = JsonTape.Parse(buffer.WrittenSpan);
        result = Convert(tape.Root, value.GetType().Name);
        return true;
    }


    private static LogEventPropertyValue Convert( JsonItem item, string? typeTag )
    {
        switch ( item.Kind )
        {
            case JsonTokenKind.Object:
                List<LogEventProperty> properties = [];
                foreach ( JsonTapeProperty property in item.EnumerateObject() ) { properties.Add(new LogEventProperty(property.GetName(), Convert(property.Value, null))); }

                return new StructureValue(properties, typeTag);

            case JsonTokenKind.Array:
                List<LogEventPropertyValue> elements = [];
                foreach ( JsonItem element in item.EnumerateArray() ) { elements.Add(Convert(element, null)); }

                return new SequenceValue(elements);

            case JsonTokenKind.String: return new ScalarValue(item.GetString());
            case JsonTokenKind.Number: return new ScalarValue(Number(item));
            case JsonTokenKind.True:   return new ScalarValue(true);
            case JsonTokenKind.False:  return new ScalarValue(false);
            default:                   return new ScalarValue(null);
        }
    }

    /// <summary> An integer when the number is one, else a <see cref="decimal"/> when it fits (keeps the written digits), else a <see cref="double"/>; integers beyond <see cref="long"/> as <see cref="BigInteger"/>, and anything bigger as its text. </summary>
    private static object Number( JsonItem item )
    {
        if ( item.TryGetInteger(out long integer) ) { return integer; }

        if ( item.TryGetFloat(out decimal number) ) { return number; }

        if ( item.TryGetFloat(out double floating) ) { return floating; }

        return item.TryGetInteger(out BigInteger big)
                   ? big
                   : item.GetRawText(); // beyond double: the number as written
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
