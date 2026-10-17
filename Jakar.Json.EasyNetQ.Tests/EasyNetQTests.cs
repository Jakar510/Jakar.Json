// Jakar.Json.EasyNetQ.Tests
// 10/17/2026

using System.Buffers;
using System.Text;
using EasyNetQ;
using EasyNetQ.Serialization.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;



namespace Jakar.Json.EasyNetQ.Tests;


[TestFixture]
public sealed class EasyNetQTests
{
    private readonly JakarJsonEasyNetQSerializer __serializer = new(new SystemTextJsonSerializer());


    [Test] public void Messages_RoundTrip_ThroughAPooledOwner()
    {
        using IMemoryOwner<byte> owner = __serializer.MessageToBytes(typeof(Order), Samples.Order());
        ReadOnlyMemory<byte>     bytes = owner.Memory;

        Assert.Multiple(() =>
                        {
                            Assert.That(Encoding.UTF8.GetString(bytes.Span),                   Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(__serializer.BytesToMessage(typeof(Order), bytes),     Is.EqualTo(Samples.Order()));
                        });
    }

    [Test] public void Collections_AndStructs()
    {
        using IMemoryOwner<byte> list  = __serializer.MessageToBytes(typeof(List<Order>), Samples.Orders());
        using IMemoryOwner<byte> point = __serializer.MessageToBytes(typeof(Point), new Point { X = 1, Y = 2 });

        Assert.Multiple(() =>
                        {
                            Assert.That(__serializer.BytesToMessage(typeof(List<Order>), list.Memory), Is.EqualTo(Samples.Orders()));
                            Assert.That(__serializer.BytesToMessage(typeof(Point), point.Memory),      Is.EqualTo(new Point { X = 1, Y = 2 }));
                        });
    }

    [Test] public void OtherTypes_UseTheFallback()
    {
        using IMemoryOwner<byte> owner = __serializer.MessageToBytes(typeof(PlainDto), new PlainDto("x"));

        Assert.Multiple(() =>
                        {
                            Assert.That(Encoding.UTF8.GetString(owner.Memory.Span),               Does.Not.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(__serializer.BytesToMessage(typeof(PlainDto), owner.Memory), Is.EqualTo(new PlainDto("x")));
                        });
    }

    [Test] public void Corrupt_Throws() => Assert.Throws<JsonReadException>(() => __serializer.BytesToMessage(typeof(Order), "{\"id\":"u8.ToArray()));

    [Test] public void UseJakarJson_ReplacesTheRegisteredSerializer()
    {
        ServiceCollection services = new();
        services.AddEasyNetQ("host=localhost").UseJakarJson(new SystemTextJsonSerializer());

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.That(provider.GetRequiredService<ISerializer>(), Is.InstanceOf<JakarJsonEasyNetQSerializer>());
    }
}
