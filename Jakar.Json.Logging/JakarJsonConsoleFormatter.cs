// Jakar.Json.Logging
// 10/17/2026

using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;



namespace Jakar.Json.Logging;


/// <summary> Options for <see cref="JakarJsonConsoleFormatter"/>: the built-in JSON formatter's, including <see cref="JsonConsoleFormatterOptions.JsonWriterOptions"/>. </summary>
public sealed class JakarJsonConsoleFormatterOptions : JsonConsoleFormatterOptions;



/// <summary>
///     Writes each log entry as one JSON object, in the shape of the built-in JSON console formatter (<c>Timestamp</c>, <c>EventId</c>, <c>LogLevel</c>, <c>Category</c>, <c>Message</c>,
///     <c>Exception</c>, <c>State</c>, <c>Scopes</c>), except that <c>[JsonModel]</c> values in the state and scopes are written as JSON objects by Jakar.Json rather than as their
///     <c>ToString()</c>. Numbers and booleans stay JSON numbers and booleans.
/// </summary>
public sealed class JakarJsonConsoleFormatter : ConsoleFormatter, IDisposable
{
    public const string NAME = "jakar-json";

    private readonly IDisposable?                     __reload;
    private          JakarJsonConsoleFormatterOptions __options;


    public JakarJsonConsoleFormatter( IOptionsMonitor<JakarJsonConsoleFormatterOptions> options ) : base(NAME)
    {
        __options = options.CurrentValue;
        __reload  = options.OnChange(o => __options = o);
    }


    public void Dispose() => __reload?.Dispose();


    public override void Write<TState>( in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter )
    {
        string? message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if ( logEntry.Exception is null && message is null ) { return; }

        JakarJsonConsoleFormatterOptions options = __options;

        using PooledBufferWriter buffer = new();

        using ( Utf8JsonWriter writer = new(buffer, options.JsonWriterOptions) )
        {
            writer.WriteStartObject();

            if ( options.TimestampFormat is { } format )
            {
                DateTimeOffset now = options.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
                writer.WriteString("Timestamp", now.ToString(format, CultureInfo.InvariantCulture));
            }

            writer.WriteNumber("EventId", logEntry.EventId.Id);
            writer.WriteString("LogLevel", logEntry.LogLevel.ToString());
            writer.WriteString("Category", logEntry.Category);
            writer.WriteString("Message", message);
            if ( logEntry.Exception is not null ) { writer.WriteString("Exception", logEntry.Exception.ToString()); }

            if ( logEntry.State is IReadOnlyList<KeyValuePair<string, object?>> state )
            {
                writer.WriteStartObject("State");
                writer.WriteString("Message", logEntry.State.ToString());
                foreach ( KeyValuePair<string, object?> item in state ) { WriteItem(writer, item.Key, item.Value); }

                writer.WriteEndObject();
            }

            if ( options.IncludeScopes && scopeProvider is not null )
            {
                writer.WriteStartArray("Scopes");
                scopeProvider.ForEachScope(static ( scope, w ) => WriteScope(w, scope), writer);
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        textWriter.Write(Encoding.UTF8.GetString(buffer.WrittenSpan));
        textWriter.Write(Environment.NewLine);
    }


    private static void WriteScope( Utf8JsonWriter writer, object? scope )
    {
        if ( scope is IEnumerable<KeyValuePair<string, object?>> items )
        {
            writer.WriteStartObject();
            writer.WriteString("Message", scope.ToString());
            foreach ( KeyValuePair<string, object?> item in items ) { WriteItem(writer, item.Key, item.Value); }

            writer.WriteEndObject();
        }
        else { WriteValue(writer, scope); }
    }

    private static void WriteItem( Utf8JsonWriter writer, string key, object? value )
    {
        writer.WritePropertyName(key);
        WriteValue(writer, value);
    }

    /// <summary> Models as Jakar.Json objects, numbers and booleans as such (NaN and infinities as text), everything else as its (invariant) text. </summary>
    private static void WriteValue( Utf8JsonWriter writer, object? value )
    {
        switch ( value )
        {
            case null:
                writer.WriteNullValue();
                return;

            case bool b:
                writer.WriteBooleanValue(b);
                return;

            case byte or sbyte or short or ushort or int or uint or long:
                writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                return;

            case ulong u:
                writer.WriteNumberValue(u);
                return;

            case decimal m:
                writer.WriteNumberValue(m);
                return;

            case float or double:
                double d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if ( double.IsFinite(d) ) { writer.WriteNumberValue(d); }
                else { writer.WriteStringValue(d.ToString(CultureInfo.InvariantCulture)); } // NaN and infinities aren't JSON numbers; a log line must never fail

                return;
        }

        if ( JsonCodecRegistry.TryGet(value.GetType(), out JsonTypeCodec? codec) )
        {
            using PooledBufferWriter model = codec.WriteToBuffer(value);
            writer.WriteRawValue(model.WrittenSpan, skipInputValidation: true);
            return;
        }

        writer.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
    }
}



public static class JakarJsonConsoleFormatterExtensions
{
    /// <summary> Logs to the console as JSON, with <c>[JsonModel]</c> values written by Jakar.Json: <c>builder.Logging.AddJakarJsonConsole()</c>. </summary>
    [RequiresUnreferencedCode("AddConsoleFormatter binds the formatter options from configuration, which may need members trimming removes")]
    [RequiresDynamicCode("AddConsoleFormatter binds the formatter options from configuration, which may need dynamic code")]
    public static ILoggingBuilder AddJakarJsonConsole( this ILoggingBuilder builder, Action<JakarJsonConsoleFormatterOptions>? configure = null )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddConsole(static o => o.FormatterName = JakarJsonConsoleFormatter.NAME).AddConsoleFormatter<JakarJsonConsoleFormatter, JakarJsonConsoleFormatterOptions>(configure ?? ( static _ => { } ));
    }
}
