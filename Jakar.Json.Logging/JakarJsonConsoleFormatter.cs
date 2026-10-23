// Jakar.Json.Logging
// 10/17/2026

using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;



namespace Jakar.Json.Logging;


/// <summary> Options for <see cref="JakarJsonConsoleFormatter"/>: the console formatter's (<c>IncludeScopes</c>, <c>TimestampFormat</c>, <c>UseUtcTimestamp</c>), plus the JSON layout. </summary>
public sealed class JakarJsonConsoleFormatterOptions : ConsoleFormatterOptions
{
    /// <summary> One entry per line when <see langword="false"/> (the default). </summary>
    public bool Indented { get; set; }

    /// <summary> Which characters are escaped besides what JSON requires (default <see cref="JsonEscaping.Minimal"/>). </summary>
    public JsonEscaping Escaping { get; set; } = JsonEscaping.Minimal;


    internal JsonWriterOptions WriterOptions => JsonWriterOptions.Default with
                                                {
                                                    Indented = Indented,
                                                    Escaping = Escaping
                                                };
}



/// <summary>
///     Writes each log entry as one JSON object, in the shape of the built-in JSON console formatter (<c>Timestamp</c>, <c>EventId</c>, <c>LogLevel</c>, <c>Category</c>, <c>Message</c>,
///     <c>Exception</c>, <c>State</c>, <c>Scopes</c>), except that <c>[JsonModel]</c> values in the state and scopes are written as JSON objects by Jakar.Json rather than as their
///     <c>ToString()</c>. Numbers and booleans stay JSON numbers and booleans. The whole entry is written by Jakar.Json.
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

        // The scope provider hands scopes to a callback, which can't hold the (ref struct) writer: collect them first.
        List<object?>? scopes = null;

        if ( options.IncludeScopes && scopeProvider is not null )
        {
            scopes = [];
            scopeProvider.ForEachScope(static ( scope, list ) => list.Add(scope), scopes);
        }

        JsonWriter writer = new(stackalloc char[1024], options.WriterOptions);

        try
        {
            writer.WriteStartObject();

            if ( options.TimestampFormat is { } format )
            {
                DateTimeOffset now = options.UseUtcTimestamp ? DateTimeOffset.UtcNow : DateTimeOffset.Now;
                writer.WritePropertyName("Timestamp");
                writer.WriteString(now.ToString(format, CultureInfo.InvariantCulture));
            }

            writer.WritePropertyName("EventId");
            writer.WriteInteger(logEntry.EventId.Id);
            writer.WritePropertyName("LogLevel");
            writer.WriteString(logEntry.LogLevel.ToString());
            writer.WritePropertyName("Category");
            writer.WriteString(logEntry.Category);
            writer.WritePropertyName("Message");
            WriteText(ref writer, message);

            if ( logEntry.Exception is not null )
            {
                writer.WritePropertyName("Exception");
                writer.WriteString(logEntry.Exception.ToString());
            }

            if ( logEntry.State is IReadOnlyList<KeyValuePair<string, object?>> state )
            {
                writer.WritePropertyName("State");
                writer.WriteStartObject();
                writer.WritePropertyName("Message");
                WriteText(ref writer, logEntry.State.ToString());
                foreach ( KeyValuePair<string, object?> item in state ) { WriteItem(ref writer, item.Key, item.Value); }

                writer.WriteEndObject();
            }

            if ( scopes is not null )
            {
                writer.WritePropertyName("Scopes");
                writer.WriteStartArray();
                foreach ( object? scope in scopes ) { WriteScope(ref writer, scope); }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();

            textWriter.Write(writer.Written);
            textWriter.Write(Environment.NewLine);
        }
        finally { writer.Dispose(); }
    }


    private static void WriteScope( ref JsonWriter writer, object? scope )
    {
        if ( scope is IEnumerable<KeyValuePair<string, object?>> items )
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Message");
            WriteText(ref writer, scope.ToString());
            foreach ( KeyValuePair<string, object?> item in items ) { WriteItem(ref writer, item.Key, item.Value); }

            writer.WriteEndObject();
        }
        else { WriteValue(ref writer, scope); }
    }

    private static void WriteItem( ref JsonWriter writer, string key, object? value )
    {
        writer.WritePropertyName(key);
        WriteValue(ref writer, value);
    }

    private static void WriteText( ref JsonWriter writer, string? text )
    {
        if ( text is null ) { writer.WriteNull(); }
        else { writer.WriteString(text); }
    }

    /// <summary> Models as Jakar.Json objects, numbers and booleans as such (NaN and infinities as text), everything else as its (invariant) text. </summary>
    private static void WriteValue( ref JsonWriter writer, object? value )
    {
        switch ( value )
        {
            case null:
                writer.WriteNull();
                return;

            case bool b:
                writer.WriteBoolean(b);
                return;

            case byte or sbyte or short or ushort or int or uint or long:
                writer.WriteInteger(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                return;

            case ulong u:
                writer.WriteInteger(u);
                return;

            case decimal m:
                writer.WriteFloat(m);
                return;

            case float or double:
                double d = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if ( double.IsFinite(d) ) { writer.WriteFloat(d); }
                else { writer.WriteString(d.ToString(CultureInfo.InvariantCulture)); } // NaN and infinities aren't JSON numbers; a log line must never fail

                return;
        }

        if ( JsonCodecRegistry.TryGet(value.GetType(), out JsonTypeCodec? codec) )
        {
            codec.WriteValue(value, ref writer);
            return;
        }

        WriteText(ref writer, Convert.ToString(value, CultureInfo.InvariantCulture));
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
