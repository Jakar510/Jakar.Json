// Jakar.Json.Flurl.Tests
// 10/17/2026

using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;



namespace Jakar.Json.Flurl.Tests;


/// <summary> A Flurl client using Jakar.Json against a Kestrel server using System.Text.Json. </summary>
[TestFixture]
public sealed class FlurlTests
{
    private LoopbackServer __server = null!;
    private IFlurlClient   __client = null!;


    [OneTimeSetUp] public async Task StartAsync()
    {
        __server = await LoopbackServer.StartAsync(static app =>
                                                   {
                                                       app.MapGet("/orders/{id:int}", static ( int id ) => TypedResults.Ok(Samples.Order(id)));
                                                       app.MapGet("/orders",          static () => TypedResults.Ok(Samples.Orders()));
                                                       app.MapPost("/orders",         static ( [FromBody] Order order ) => TypedResults.Ok(order with { Id = 99 }));
                                                       app.MapPost("/plain",          static ( [FromBody] PlainDto dto ) => TypedResults.Ok(dto with { Name = dto.Name + "!" }));
                                                       app.MapPost("/echo", static async ( HttpRequest request ) =>
                                                                            {
                                                                                using StreamReader reader = new(request.Body);
                                                                                return Results.Text(await reader.ReadToEndAsync());
                                                                            });
                                                   });

        __client = new FlurlClient(__server.Address.ToString());
        __client.Settings.UseJakarJson();
    }

    [OneTimeTearDown] public async Task StopAsync()
    {
        __client.Dispose();
        await __server.DisposeAsync();
    }


    [Test] public void UseJakarJson_WrapsTheExistingSerializer_Once()
    {
        FlurlHttpSettings settings = new FlurlClient().Settings.UseJakarJson().UseJakarJson();

        Assert.Multiple(() =>
                        {
                            Assert.That(settings.JsonSerializer,                                     Is.InstanceOf<JakarJsonFlurlSerializer>());
                            Assert.That(((JakarJsonFlurlSerializer)settings.JsonSerializer).Fallback, Is.InstanceOf<DefaultJsonSerializer>());
                        });
    }

    [Test] public async Task Get_ReadsModelsFromTheStream()
    {
        await Assert.MultipleAsync(async () =>
                                   {
                                       Assert.That(await __client.Request("orders", 3).GetJsonAsync<Order>(),     Is.EqualTo(Samples.Order(3)));
                                       Assert.That(await __client.Request("orders").GetJsonAsync<List<Order>>(),  Is.EqualTo(Samples.Orders()));
                                   });
    }

    [Test] public async Task Post_WritesJakarJson_TheServerReadsIt()
    {
        await Assert.MultipleAsync(async () =>
                                   {
                                       Assert.That(await __client.Request("orders").PostJsonAsync(Samples.Order()).ReceiveJson<Order>(), Is.EqualTo(Samples.Order(99)));
                                       Assert.That(await __client.Request("echo").PostJsonAsync(Samples.Order()).ReceiveString(),       Does.Contain(Samples.JAKAR_SEPARATOR));
                                   });
    }

    [Test] public async Task NonModels_UseTheFallback() => Assert.That(await __client.Request("plain").PostJsonAsync(new PlainDto("x")).ReceiveJson<PlainDto>(), Is.EqualTo(new PlainDto("x!")));
}
