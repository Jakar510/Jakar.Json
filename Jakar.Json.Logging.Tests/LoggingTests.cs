// Jakar.Json.Logging.Tests
// 10/17/2026

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

    private static JNode Write<TState>( JakarJsonConsoleFormatter formatter, TState state, Func<TState, Exception?, string> format, IExternalScopeProvider? scopes = null, Exception? exception = null )
    {
        using StringWriter writer = new();
        formatter.Write(new LogEntry<TState>(LogLevel.Warning, "Orders", new EventId(7, "placed"), state, exception, format), scopes, writer);

        string line = writer.ToString();
        Assert.That(line, Does.EndWith(Environment.NewLine).And.Not.Contain("\n" + "{"), "one entry per line");
        return JNode.Parse(line)!;
    }

    private static IReadOnlyList<KeyValuePair<string, object?>> State( params (string Key, object? Value)[] items ) => items.Select(static i => new KeyValuePair<string, object?>(i.Key, i.Value)).ToList();


    // ─── Unit ────────────────────────────────────────────────────────────────

    [Test] public void Entry_HasTheBuiltInShape_AndModelsAreObjects()
    {
        IReadOnlyList<KeyValuePair<string, object?>> state = State(("Order", Samples.Order()), ("Count", 3), ("Paid", true), ("Note", "rush"), ("{OriginalFormat}", "Placed {Order}"));
        JNode                                        json  = Write(Formatter(), state, static ( _, _ ) => "Placed order 7");
        JNode                                        st    = json["State"]!;

        Assert.Multiple(() =>
                        {
                            Assert.That(json["EventId"]!.AsValue().GetInt32(),                         Is.EqualTo(7));
                            Assert.That(json["LogLevel"]!.AsValue().GetString(),                       Is.EqualTo("Warning"));
                            Assert.That(json["Category"]!.AsValue().GetString(),                       Is.EqualTo("Orders"));
                            Assert.That(json["Message"]!.AsValue().GetString(),                        Is.EqualTo("Placed order 7"));
                            Assert.That(json.AsObject().ContainsKey("Timestamp"),                        Is.True);
                            Assert.That(st["Order"]!.Kind,                                   Is.EqualTo(JNodeKind.Object), "a model, not its ToString()");
                            Assert.That(st["Order"]!["customer"]!.AsValue().GetString(),   Is.EqualTo("Zoë"));
                            Assert.That(st["Count"]!.AsValue().GetInt32(),                            Is.EqualTo(3));
                            Assert.That(st["Paid"]!.AsValue().GetBoolean(),                           Is.True);
                            Assert.That(st["Note"]!.AsValue().GetString(),                            Is.EqualTo("rush"));
                        });
    }

    [Test] public void Collections_AreArrays_ExceptionsAreText()
    {
        JNode json = Write(Formatter(), State(("Orders", Samples.Orders())), static ( _, _ ) => "batch", exception: new InvalidOperationException("boom"));

        Assert.Multiple(() =>
                        {
                            Assert.That(json["State"]!["Orders"]!.AsArray().Count, Is.EqualTo(2));
                            Assert.That(json["Exception"]!.AsValue().GetString(),                       Does.Contain("boom"));
                        });
    }

    [Test] public void Scopes_ModelsAreObjects()
    {
        LoggerExternalScopeProvider scopes = new();
        using IDisposable?           scope  = scopes.Push(State(("Order", Samples.Order())));

        JNode json = Write(Formatter(scopes: true), "plain", static ( s, _ ) => s, scopes);

        Assert.That(json["Scopes"]![0]!["Order"]!["id"]!.AsValue().GetInt32(), Is.EqualTo(7));
    }

    [Test] public void NonFiniteDoubles_AreText_TheLineStaysValid()
    {
        JNode state = Write(Formatter(), State(("Ratio", double.NaN), ("Rate", 0.25)), static ( _, _ ) => "x")["State"]!;

        Assert.Multiple(() =>
                        {
                            Assert.That(state["Ratio"]!.AsValue().GetString(), Is.EqualTo("NaN"));
                            Assert.That(state["Rate"]!.AsValue().GetDouble(),  Is.EqualTo(0.25));
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
