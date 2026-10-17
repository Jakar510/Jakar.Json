// Jakar.Json.Serilog.Tests
// 10/17/2026

using Serilog;
using Serilog.Core;
using Serilog.Events;



namespace Jakar.Json.Serilog.Tests;


[TestFixture]
public sealed class SerilogTests
{
    private static (Logger Logger, List<LogEvent> Events) Create()
    {
        List<LogEvent> events = [];
        Logger         logger = new LoggerConfiguration().Destructure.UsingJakarJson().WriteTo.Sink(new CollectingSink(events)).CreateLogger();
        return ( logger, events );
    }


    [Test] public void Models_DestructureWithJakarJsonsShape()
    {
        ( Logger logger, List<LogEvent> events ) = Create();
        using ( logger ) { logger.Information("Placed {@Order}", Samples.Order()); }

        StructureValue order = (StructureValue)events.Single().Properties["Order"];
        StructureValue line  = (StructureValue)( (SequenceValue)order.Properties.Single(static p => p.Name == "lines").Value ).Elements.Single();

        Assert.Multiple(() =>
                        {
                            Assert.That(order.TypeTag,                                Is.EqualTo(nameof(Order)));
                            Assert.That(order.Properties.Select(static p => p.Name), Is.EqualTo(new[] { "id", "customer", "total", "lines" }), "Jakar.Json's names, in its order");
                            Assert.That(Scalar(order, "customer"),                   Is.EqualTo("Zoë"));
                            Assert.That(Scalar(order, "id"),                         Is.EqualTo(7L));
                            Assert.That(Scalar(order, "total"),                      Is.EqualTo(12.50m));
                            Assert.That(Scalar(line, "sku"),                         Is.EqualTo("A-1"));
                        });
    }

    [Test] public void Collections_AreSequences()
    {
        ( Logger logger, List<LogEvent> events ) = Create();
        using ( logger ) { logger.Information("Batch {@Orders}", Samples.Orders()); }

        Assert.That(( (SequenceValue)events.Single().Properties["Orders"] ).Elements, Has.Count.EqualTo(2));
    }

    [Test] public void OtherTypes_KeepSerilogsDestructuring()
    {
        ( Logger logger, List<LogEvent> events ) = Create();
        using ( logger ) { logger.Information("Plain {@Dto}", new PlainDto("x")); }

        StructureValue dto = (StructureValue)events.Single().Properties["Dto"];
        Assert.That(dto.Properties.Single().Name, Is.EqualTo("Name"), "reflection, the C# name");
    }

    [Test] public void WithoutDestructuring_IsToString()
    {
        ( Logger logger, List<LogEvent> events ) = Create();
        using ( logger ) { logger.Information("Placed {Order}", Samples.Order()); }

        Assert.That(events.Single().Properties["Order"], Is.InstanceOf<ScalarValue>());
    }


    private static object? Scalar( StructureValue structure, string name ) => ( (ScalarValue)structure.Properties.Single(p => p.Name == name).Value ).Value;



    private sealed class CollectingSink( List<LogEvent> events ) : ILogEventSink
    {
        public void Emit( LogEvent logEvent ) => events.Add(logEvent);
    }
}
