// Jakar.Json.Akka.Tests
// 10/17/2026

using System.Text;
using Akka.Actor;
using Akka.Actor.Setup;
using Akka.Configuration;
using Akka.Serialization;



namespace Jakar.Json.Akka.Tests;


public sealed class EchoActor : ReceiveActor
{
    public EchoActor() => Receive<Order>(order => Sender.Tell(order with { Customer = order.Customer + "!" }));
}



[TestFixture]
public sealed class AkkaTests
{
    [Test] public async Task Setup_BindsModels_AndMessagesAreSerialized()
    {
        // serialize-messages makes Akka serialize every local message, as remoting and persistence do.
        ActorSystemSetup setup = BootstrapSetup.Create().WithConfig(ConfigurationFactory.ParseString("akka.actor.serialize-messages = on")).And(JakarJsonAkka.Setup(typeof(Order)));

        using ActorSystem system = ActorSystem.Create("jakar", setup);
        Serializer        found  = system.Serialization.FindSerializerFor(Samples.Order());
        Order             reply  = await system.ActorOf(Props.Create<EchoActor>()).Ask<Order>(Samples.Order(), TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
                        {
                            Assert.That(found,           Is.InstanceOf<JakarJsonAkkaSerializer>());
                            Assert.That(reply.Customer,  Is.EqualTo("Zoë!"));
                        });

        await system.Terminate();
    }

    [Test] public async Task Hocon_RegistersTheSerializer_AndRoundTrips()
    {
        Config config = ConfigurationFactory.ParseString(JakarJsonAkka.Hocon + "\n" + $$"""akka.actor.serialization-bindings { "{{typeof(Order).AssemblyQualifiedName}}" = jakar-json }""");

        using ActorSystem system     = ActorSystem.Create("jakar-hocon", config);
        Serializer        serializer = system.Serialization.FindSerializerFor(Samples.Order());
        byte[]            bytes      = serializer.ToBinary(Samples.Order());
        string            manifest   = Serialization.ManifestFor(serializer, Samples.Order());

        Assert.Multiple(() =>
                        {
                            Assert.That(serializer.Identifier,                                                      Is.EqualTo(JakarJsonAkkaSerializer.ID));
                            Assert.That(Encoding.UTF8.GetString(bytes),                                             Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(system.Serialization.Deserialize(bytes, serializer.Identifier, manifest),  Is.EqualTo(Samples.Order()));
                        });

        await system.Terminate();
    }

    [Test] public void Unsupported_Throws()
    {
        using ActorSystem system = ActorSystem.Create("jakar-unsupported");
        Assert.Throws<NotSupportedException>(() => new JakarJsonAkkaSerializer((ExtendedActorSystem)system).ToBinary(new PlainDto("x")));
    }
}
