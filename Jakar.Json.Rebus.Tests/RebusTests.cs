// Jakar.Json.Rebus.Tests
// 10/17/2026

using System.Text;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Routing.TypeBased;
using Rebus.Transport.InMem;



namespace Jakar.Json.Rebus.Tests;


/// <summary> End to end through Rebus' in-memory transport, which carries serialized transport messages. </summary>
[TestFixture]
public sealed class RebusTests
{
    [Test] public async Task Send_BodyIsJakarJson()
    {
        InMemNetwork network = new();

        using IBus client = Configure.OneWayClient()
                                     .Transport(t => t.UseInMemoryTransportAsOneWayClient(network))
                                     .Serialization(static s => s.UseJakarJson())
                                     .Routing(static r => r.TypeBased().Map<Order>("orders"))
                                     .Start();

        network.CreateQueue("orders");
        await client.Send(Samples.Order());

        InMemTransportMessage? queued = network.GetNextOrNull("orders");
        Assert.That(Encoding.UTF8.GetString(queued!.Body), Does.Contain(Samples.JAKAR_SEPARATOR));
    }

    [Test] public async Task SendLocal_HandlerReceivesTheModel()
    {
        using BuiltinHandlerActivator activator = new();
        TaskCompletionSource<Order>   received  = new(TaskCreationOptions.RunContinuationsAsynchronously);
        activator.Register(() => new Handler<Order>(received));

        using IBus bus = Configure.With(activator)
                                  .Transport(static t => t.UseInMemoryTransport(new InMemNetwork(), "orders"))
                                  .Serialization(static s => s.UseJakarJson())
                                  .Start();

        await bus.SendLocal(Samples.Order());
        Assert.That(await received.Task.WaitAsync(TimeSpan.FromSeconds(10)), Is.EqualTo(Samples.Order()));
    }

    [Test] public async Task OtherTypes_KeepSystemTextJson()
    {
        using BuiltinHandlerActivator  activator = new();
        TaskCompletionSource<PlainDto> received  = new(TaskCreationOptions.RunContinuationsAsynchronously);
        activator.Register(() => new Handler<PlainDto>(received));

        using IBus bus = Configure.With(activator)
                                  .Transport(static t => t.UseInMemoryTransport(new InMemNetwork(), "plain"))
                                  .Serialization(static s => s.UseJakarJson())
                                  .Start();

        await bus.SendLocal(new PlainDto("x"));
        Assert.That(await received.Task.WaitAsync(TimeSpan.FromSeconds(10)), Is.EqualTo(new PlainDto("x")));
    }



    private sealed class Handler<T>( TaskCompletionSource<T> received ) : IHandleMessages<T>
    {
        public Task Handle( T message )
        {
            received.TrySetResult(message);
            return Task.CompletedTask;
        }
    }
}
