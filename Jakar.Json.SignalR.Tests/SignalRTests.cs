// Jakar.Json.SignalR.Tests
// 10/17/2026

using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;



namespace Jakar.Json.SignalR.Tests;


public sealed class OrdersHub : Hub
{
    public Order       Reprice( Order order )       => order with { Total = order.Total + 1 };
    public List<Order> Batch( List<Order> orders )  => orders.Select(static o => o with { Id = -o.Id }).ToList();
    public PlainDto    Plain( PlainDto dto )        => dto with { Name = dto.Name + "!" };

    public Task Broadcast( Order order ) => Clients.Caller.SendAsync("updated", order);
}



[TestFixture]
public sealed class ProtocolTests
{
    // ─── Unit: the hub protocol itself ───────────────────────────────────────

    [Test] public void Invocation_ArgumentsAreJakarJson_AndParseBack()
    {
        JsonHubProtocol         protocol = new(Options.Create(Configured()));
        ArrayBufferWriter<byte> buffer   = new();
        protocol.WriteMessage(new InvocationMessage("1", "Reprice", [Samples.Order()]), buffer);

        ReadOnlySequence<byte> frame = new(buffer.WrittenMemory);
        bool parsed = protocol.TryParseMessage(ref frame, new Binder(typeof(Order)), out HubMessage? message);

        Assert.Multiple(() =>
                        {
                            Assert.That(Encoding.UTF8.GetString(buffer.WrittenSpan),            Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(parsed,                                               Is.True);
                            Assert.That(( (InvocationMessage)message! ).Arguments.Single(),    Is.EqualTo(Samples.Order()));
                        });
    }

    [Test] public void AddJakarJsonProtocol_ConfiguresTheOptions()
    {
        JsonHubProtocolOptions options = Configured();
        Assert.That(options.PayloadSerializerOptions.Converters.OfType<Jakar.Json.SystemTextJson.JakarJsonConverterFactory>(), Is.Not.Empty);
    }


    internal static JsonHubProtocolOptions Configured() => new ServiceCollection().AddSignalRCore().AddJakarJsonProtocol().Services.BuildServiceProvider().GetRequiredService<IOptions<JsonHubProtocolOptions>>().Value;



    private sealed class Binder( Type argument ) : IInvocationBinder
    {
        public IReadOnlyList<Type> GetParameterTypes( string methodName ) => [argument];
        public Type                GetReturnType( string invocationId ) => argument;
        public Type                GetStreamItemType( string streamId ) => argument;
    }
}



/// <summary> End to end: a Kestrel hub and a .NET client, over WebSockets. </summary>
[TestFixture]
public sealed class HubTests
{
    private LoopbackServer __server = null!;


    [OneTimeSetUp] public async Task StartAsync() => __server = await LoopbackServer.StartAsync(static app => app.MapHub<OrdersHub>("/orders"),
                                                                                                static builder => builder.Services.AddSignalR().AddJakarJsonProtocol());

    [OneTimeTearDown] public async Task StopAsync() => await __server.DisposeAsync();

    private async Task<HubConnection> ConnectAsync( bool jakar )
    {
        IHubConnectionBuilder builder = new HubConnectionBuilder().WithUrl(new Uri(__server.Address, "orders"));
        HubConnection connection = ( jakar ? builder.AddJakarJsonProtocol() : builder ).Build();
        await connection.StartAsync();
        return connection;
    }


    [Test] public async Task Invoke_ModelsRoundTrip()
    {
        await using HubConnection connection = await ConnectAsync(jakar: true);

        await Assert.MultipleAsync(async () =>
                                   {
                                       Assert.That(await connection.InvokeAsync<Order>("Reprice", Samples.Order()),          Is.EqualTo(Samples.Order() with { Total = 13.50m }));
                                       Assert.That(await connection.InvokeAsync<List<Order>>("Batch", Samples.Orders()),     Is.EqualTo(Samples.Orders().Select(static o => o with { Id = -o.Id }).ToList()));
                                       Assert.That(await connection.InvokeAsync<PlainDto>("Plain", new PlainDto("x")),       Is.EqualTo(new PlainDto("x!")), "other types keep System.Text.Json");
                                   });
    }

    [Test] public async Task ServerToClient_Messages()
    {
        await using HubConnection connection = await ConnectAsync(jakar: true);
        TaskCompletionSource<Order> received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<Order>("updated", order => received.TrySetResult(order));

        await connection.InvokeAsync("Broadcast", Samples.Order(5));
        Assert.That(await received.Task.WaitAsync(TimeSpan.FromSeconds(10)), Is.EqualTo(Samples.Order(5)));
    }

    [Test] public async Task Interop_ClientWithoutJakarJson()
    {
        // The protocol is still "json": a client using plain System.Text.Json talks to the Jakar.Json server.
        await using HubConnection connection = await ConnectAsync(jakar: false);
        Assert.That(await connection.InvokeAsync<Order>("Reprice", Samples.Order()), Is.EqualTo(Samples.Order() with { Total = 13.50m }));
    }
}
