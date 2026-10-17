// Jakar.Json.MassTransit.Tests
// 10/17/2026

using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;



namespace Jakar.Json.MassTransit.Tests;


public sealed class OrderConsumer : IConsumer<Order>
{
    public static readonly List<(Order Message, string Body)> Received = [];

    public Task Consume( ConsumeContext<Order> context )
    {
        lock ( Received ) { Received.Add(( context.Message, context.ReceiveContext.Body.GetString() )); }

        return Task.CompletedTask;
    }
}



public sealed class PlainConsumer : IConsumer<PlainDto>
{
    public static readonly List<(PlainDto Message, string Body)> Received = [];

    public Task Consume( ConsumeContext<PlainDto> context )
    {
        lock ( Received ) { Received.Add(( context.Message, context.ReceiveContext.Body.GetString() )); }

        return Task.CompletedTask;
    }
}



/// <summary> Answers an order request with the order, totalled. </summary>
public sealed class PricingConsumer : IConsumer<Order>
{
    public Task Consume( ConsumeContext<Order> context ) => context.RespondAsync(context.Message with { Total = context.Message.Lines.Sum(static l => l.Price * l.Quantity) + 1 });
}



/// <summary> End to end through MassTransit's in-memory transport, which serializes every message. </summary>
[TestFixture, NonParallelizable]
public sealed class MassTransitTests
{
    [Test] public async Task Published_Model_IsJakarJsonInsideTheEnvelope()
    {
        await using ServiceProvider provider = new ServiceCollection().AddMassTransitTestHarness(static x =>
                                                                                                  {
                                                                                                      x.AddConsumer<OrderConsumer>();
                                                                                                      x.UsingInMemory(static ( context, cfg ) =>
                                                                                                                      {
                                                                                                                          cfg.UseJakarJson();
                                                                                                                          cfg.ConfigureEndpoints(context);
                                                                                                                      });
                                                                                                  })
                                                                                      .BuildServiceProvider(true);

        ITestHarness harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(Samples.Order());

            Assert.That(await harness.GetConsumerHarness<OrderConsumer>().Consumed.Any<Order>(), Is.True);

            (Order message, string body) = OrderConsumer.Received.Last();

            Assert.Multiple(() =>
                            {
                                Assert.That(message, Is.EqualTo(Samples.Order()));
                                Assert.That(body,    Does.Contain("\"message\"").And.Contain("\"messageType\""), "MassTransit's envelope");
                                Assert.That(body,    Does.Contain(Samples.JAKAR_SEPARATOR),                          "the message itself is written by Jakar.Json");
                            });
        }
        finally { await harness.Stop(); }
    }

    [Test] public async Task NonModels_KeepSystemTextJson()
    {
        await using ServiceProvider provider = Provider(static x => x.AddConsumer<PlainConsumer>());
        ITestHarness                harness  = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            await harness.Bus.Publish(new PlainDto("x"));
            Assert.That(await harness.GetConsumerHarness<PlainConsumer>().Consumed.Any<PlainDto>(), Is.True);

            (PlainDto message, string body) = PlainConsumer.Received.Last();

            Assert.Multiple(() =>
                            {
                                Assert.That(message, Is.EqualTo(new PlainDto("x")));
                                Assert.That(body,    Does.Not.Contain(Samples.JAKAR_SEPARATOR));
                            });
        }
        finally { await harness.Stop(); }
    }

    [Test] public async Task RequestResponse_RoundTripsModels()
    {
        await using ServiceProvider provider = Provider(static x =>
                                                        {
                                                            x.AddConsumer<PricingConsumer>();
                                                            x.AddRequestClient<Order>();
                                                        });

        ITestHarness harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        try
        {
            Response<Order> response = await harness.GetRequestClient<Order>().GetResponse<Order>(Samples.Order());
            Assert.That(response.Message, Is.EqualTo(Samples.Order() with { Total = 13.50m }));
        }
        finally { await harness.Stop(); }
    }


    private static ServiceProvider Provider( Action<IBusRegistrationConfigurator> consumers ) => new ServiceCollection().AddMassTransitTestHarness(x =>
                                                                                                                                                   {
                                                                                                                                                       consumers(x);
                                                                                                                                                       x.UsingInMemory(static ( context, cfg ) =>
                                                                                                                                                                       {
                                                                                                                                                                           cfg.UseJakarJson();
                                                                                                                                                                           cfg.ConfigureEndpoints(context);
                                                                                                                                                                       });
                                                                                                                                                   })
                                                                                                                     .BuildServiceProvider(true);
}