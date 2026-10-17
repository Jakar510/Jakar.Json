// Jakar.Json.Orleans.Tests
// 10/17/2026

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Serialization;



namespace Jakar.Json.Orleans.Tests;


/// <summary> Orleans' serializer, configured as a silo or client configures it. </summary>
[TestFixture]
public sealed class OrleansTests
{
    private static Serializer Serializer() => new ServiceCollection().AddSerializer(static s => s.AddJakarJsonSerializer()).BuildServiceProvider().GetRequiredService<Serializer>();


    [Test] public void Models_AreJakarJson_InsideOrleansFraming()
    {
        Serializer serializer = Serializer();
        byte[]     bytes      = serializer.SerializeToArray(Samples.Order());

        Assert.Multiple(() =>
                        {
                            Assert.That(Encoding.UTF8.GetString(bytes),          Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(serializer.Deserialize<Order>(bytes),    Is.EqualTo(Samples.Order()));
                        });
    }

    [Test] public void Collections_AndStructs()
    {
        Serializer serializer = Serializer();

        Assert.Multiple(() =>
                        {
                            Assert.That(serializer.Deserialize<List<Order>>(serializer.SerializeToArray(Samples.Orders())),     Is.EqualTo(Samples.Orders()));
                            Assert.That(serializer.Deserialize<Point>(serializer.SerializeToArray(new Point { X = 1, Y = 2 })), Is.EqualTo(new Point { X = 1, Y = 2 }));
                        });
    }

    [Test] public void OtherTypes_KeepOrleansSerializers()
    {
        Serializer serializer = Serializer();
        byte[]     bytes      = serializer.SerializeToArray("plain text");

        Assert.Multiple(() =>
                        {
                            Assert.That(Encoding.UTF8.GetString(bytes),          Does.Not.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(serializer.Deserialize<string>(bytes),   Is.EqualTo("plain text"));
                        });
    }
}
