// Jakar.Json.NServiceBus.Tests
// 10/17/2026

using System.Text;
using NServiceBus;
using NServiceBus.MessageMutator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;



namespace Jakar.Json.NServiceBus.Tests;


public sealed class OrderHandler : IHandleMessages<Order>
{
    public static TaskCompletionSource<Order> Received = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task Handle( Order message, IMessageHandlerContext context )
    {
        Received.TrySetResult(message);
        return Task.CompletedTask;
    }
}



public sealed class BodyCapture : IMutateIncomingTransportMessages
{
    public static TaskCompletionSource<string> Body = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task MutateIncoming( MutateIncomingTransportMessageContext context )
    {
        Body.TrySetResult(Encoding.UTF8.GetString(context.Body.Span));
        return Task.CompletedTask;
    }
}



/// <summary> A real endpoint, hosted by the generic host, on NServiceBus' file-based learning transport. </summary>
[TestFixture, NonParallelizable]
public sealed class NServiceBusTests
{
    [Test] public async Task SendLocal_IsJakarJson_AndHandled()
    {
        string storage = Path.Combine(Path.GetTempPath(), "jakar-nsb-" + Guid.NewGuid().ToString("N"));

        EndpointConfiguration configuration = new("JakarJsonTests");
        configuration.UseTransport(new LearningTransport { StorageDirectory = storage });
        configuration.UseJakarJsonSerialization();
        configuration.Conventions().DefiningCommandsAs(static t => t == typeof(Order));
        configuration.RegisterMessageMutator(new BodyCapture());
        configuration.SendFailedMessagesTo("error");
        configuration.AssemblyScanner().ExcludeAssemblies("Jakar.Json.SystemTextJson");

        HostApplicationBuilder builder = Host.CreateApplicationBuilder();
        builder.Services.AddNServiceBusEndpoint(configuration);
        using IHost host = builder.Build();
        await host.StartAsync();

        try
        {
            await host.Services.GetRequiredService<IMessageSession>().SendLocal(Samples.Order());

            Order  received = await OrderHandler.Received.Task.WaitAsync(TimeSpan.FromSeconds(30));
            string body     = await BodyCapture.Body.Task.WaitAsync(TimeSpan.FromSeconds(30));

            Assert.Multiple(() =>
                            {
                                Assert.That(received, Is.EqualTo(Samples.Order()));
                                Assert.That(body,     Does.Contain(Samples.JAKAR_SEPARATOR));
                            });
        }
        finally
        {
            await host.StopAsync();
            try { Directory.Delete(storage, true); }
            catch ( IOException ) { }
        }
    }
}
