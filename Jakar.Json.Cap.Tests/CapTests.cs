// Jakar.Json.Cap.Tests
// 10/17/2026

using System.Text;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Serialization;
using Microsoft.Extensions.DependencyInjection;



namespace Jakar.Json.Cap.Tests;


/// <summary> CAP's registered serializer, as its publisher and consumers use it. </summary>
[TestFixture]
public sealed class CapTests
{
    private static ISerializer Serializer()
    {
        ServiceCollection services = new();
        services.AddCap(static x => x.UseJakarJson());
        return services.BuildServiceProvider().GetRequiredService<ISerializer>();
    }


    [Test] public async Task Payloads_AreJakarJson_AndRoundTrip()
    {
        ISerializer      serializer = Serializer();
        Message          message    = new(new Dictionary<string, string?> { [Headers.MessageId] = "1", [Headers.MessageName] = "orders" }, Samples.Order());
        TransportMessage transport  = await serializer.SerializeAsync(message);
        Message          back       = await serializer.DeserializeAsync(transport, typeof(Order));

        Assert.Multiple(() =>
                        {
                            Assert.That(Encoding.UTF8.GetString(transport.Body.Span), Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(back.Value,                                  Is.EqualTo(Samples.Order()));
                        });
    }

    [Test] public async Task OtherTypes_KeepSystemTextJson()
    {
        ISerializer      serializer = Serializer();
        TransportMessage transport  = await serializer.SerializeAsync(new Message(new Dictionary<string, string?> { [Headers.MessageId] = "2", [Headers.MessageName] = "plain" }, new PlainDto("x")));

        Assert.Multiple(async () =>
                        {
                            Assert.That(Encoding.UTF8.GetString(transport.Body.Span),                     Does.Not.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(( await serializer.DeserializeAsync(transport, typeof(PlainDto)) ).Value, Is.EqualTo(new PlainDto("x")));
                        });
    }
}
