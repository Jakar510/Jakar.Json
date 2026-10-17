// Jakar.Json.Wolverine.Tests
// 10/17/2026

using System.Text;
using Wolverine;
using Wolverine.Runtime.Serialization;



namespace Jakar.Json.Wolverine.Tests;


[TestFixture]
public sealed class WolverineTests
{
    private static JakarJsonWolverineSerializer Serializer()
    {
        WolverineOptions options = new();
        options.UseJakarJson();
        return (JakarJsonWolverineSerializer)options.DefaultSerializer;
    }


    [Test] public void UseJakarJson_WrapsTheDefaultSerializer_Once()
    {
        WolverineOptions   options  = new();
        IMessageSerializer original = options.DefaultSerializer;
        options.UseJakarJson().UseJakarJson();

        Assert.Multiple(() =>
                        {
                            Assert.That(options.DefaultSerializer,                                       Is.InstanceOf<JakarJsonWolverineSerializer>());
                            Assert.That(((JakarJsonWolverineSerializer)options.DefaultSerializer).Fallback, Is.SameAs(original));
                            Assert.That(options.DefaultSerializer.ContentType,                            Is.EqualTo("application/json"));
                        });
    }

    [Test] public void Messages_RoundTrip_FromTheEnvelopeBody()
    {
        JakarJsonWolverineSerializer serializer = Serializer();
        byte[]                       body       = serializer.Write(new Envelope(Samples.Order()));

        Envelope incoming = new();
        incoming.CopyBodyFrom(body);

        Assert.Multiple(() =>
                        {
                            Assert.That(Encoding.UTF8.GetString(body),                       Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(serializer.ReadFromData(typeof(Order), incoming),     Is.EqualTo(Samples.Order()));
                        });
    }

    [Test] public void OtherTypes_UseTheFallback()
    {
        JakarJsonWolverineSerializer serializer = Serializer();
        byte[]                       body       = serializer.Write(new Envelope(new PlainDto("x")));

        Envelope incoming = new();
        incoming.CopyBodyFrom(body);

        Assert.Multiple(() =>
                        {
                            Assert.That(Encoding.UTF8.GetString(body),                       Does.Not.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(serializer.ReadFromData(typeof(PlainDto), incoming),  Is.EqualTo(new PlainDto("x")));
                        });
    }
}
