// Jakar.Json.Logging.Tests
// 10/17/2026

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;



namespace Jakar.Json.Logging.Tests;


[TestFixture]
public sealed class LoggingTests
{
    private static JakarJsonConsoleFormatter Formatter( bool scopes = false ) => new(new StaticOptions(new JakarJsonConsoleFormatterOptions { IncludeScopes = scopes, TimestampFormat = "O" }));

    private static JsonElement Write<TState>( JakarJsonConsoleFormatter formatter, TState state, Func<TState, Exception?, string> format, IExternalScopeProvider? scopes = null, Exception? exception = null )
    {
        using StringWriter writer = new();
        formatter.Write(new LogEntry<TState>(LogLevel.Warning, "Orders", new EventId(7, "placed"), state, exception, format), scopes, writer);
        return JsonDocument.Parse(writer.ToString()).RootElement.Clone();
    }

    private static IReadOnlyList<KeyValuePair<string, object?>> State( params (string Key, object? Value)[] items ) => items.Select(static i => new KeyValuePair<string, object?>(i.Key, i.Value)).ToList();


    // ─── Unit ────────────────────────────────────────────────────────────────

    [Test] public void Entry_HasTheBuiltInShape_AndModelsAreObjects()
    {
        IReadOnlyList<KeyValuePair<string, object?>> state = State(("Order", Samples.Order()), ("Count", 3), ("Paid", true), ("Note", "rush"), ("{OriginalFormat}", "Placed {Order}"));
        JsonElement                                  json  = Write(Formatter(), state, static ( _, _ ) => "Placed order 7");
        JsonElement                                  st    = json.GetProperty("State");

        Assert.Multiple(() =>
                        {
                            Assert.That(json.GetProperty("EventId").GetInt32(),                         Is.EqualTo(7));
                            Assert.That(json.GetProperty("LogLevel").GetString(),                       Is.EqualTo("Warning"));
                            Assert.That(json.GetProperty("Category").GetString(),                       Is.EqualTo("Orders"));
                            Assert.That(json.GetProperty("Message").GetString(),                        Is.EqualTo("Placed order 7"));
                            Assert.That(json.TryGetProperty("Timestamp", out _),                        Is.True);
                            Assert.That(st.GetProperty("Order").ValueKind,                              Is.EqualTo(JsonValueKind.Object), "a model, not its ToString()");
                            Assert.That(st.GetProperty("Order").GetProperty("customer").GetString(),   Is.EqualTo("Zoë"));
                            Assert.That(st.GetProperty("Count").GetInt32(),                            Is.EqualTo(3));
                            Assert.That(st.GetProperty("Paid").GetBoolean(),                           Is.True);
                            Assert.That(st.GetProperty("Note").GetString(),                            Is.EqualTo("rush"));
                        });
    }

    [Test] public void Collections_AreArrays_ExceptionsAreText()
    {
        JsonElement json = Write(Formatter(), State(("Orders", Samples.Orders())), static ( _, _ ) => "batch", exception: new InvalidOperationException("boom"));

        Assert.Multiple(() =>
                        {
                            Assert.That(json.GetProperty("State").GetProperty("Orders").GetArrayLength(), Is.EqualTo(2));
                            Assert.That(json.GetProperty("Exception").GetString(),                       Does.Contain("boom"));
                        });
    }

    [Test] public void Scopes_ModelsAreObjects()
    {
        LoggerExternalScopeProvider scopes = new();
        using IDisposable?           scope  = scopes.Push(State(("Order", Samples.Order())));

        JsonElement json = Write(Formatter(scopes: true), "plain", static ( s, _ ) => s, scopes);

        Assert.That(json.GetProperty("Scopes")[0].GetProperty("Order").GetProperty("id").GetInt32(), Is.EqualTo(7));
    }

    [Test] public void NonFiniteDoubles_AreText_TheLineStaysValid()
    {
        JsonElement state = Write(Formatter(), State(("Ratio", double.NaN), ("Rate", 0.25)), static ( _, _ ) => "x").GetProperty("State");

        Assert.Multiple(() =>
                        {
                            Assert.That(state.GetProperty("Ratio").GetString(), Is.EqualTo("NaN"));
                            Assert.That(state.GetProperty("Rate").GetDouble(),  Is.EqualTo(0.25));
                        });
    }


    // ─── Integration: through LoggerFactory and the console logger ───────────

    [Test] public void AddJakarJsonConsole_RegistersTheFormatter()
    {
        ServiceCollection services = new();
        services.AddLogging(static b => b.AddJakarJsonConsole(static o => o.IncludeScopes = true));
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Multiple(() =>
                        {
                            Assert.That(provider.GetServices<ConsoleFormatter>().Select(static f => f.Name), Does.Contain(JakarJsonConsoleFormatter.NAME));
                            Assert.That(provider.GetRequiredService<IOptions<ConsoleLoggerOptions>>().Value.FormatterName, Is.EqualTo(JakarJsonConsoleFormatter.NAME));
                        });
    }



    private sealed class StaticOptions( JakarJsonConsoleFormatterOptions value ) : IOptionsMonitor<JakarJsonConsoleFormatterOptions>
    {
        public JakarJsonConsoleFormatterOptions CurrentValue => value;
        public JakarJsonConsoleFormatterOptions Get( string? name ) => value;
        public IDisposable? OnChange( Action<JakarJsonConsoleFormatterOptions, string?> listener ) => null;
    }
}
