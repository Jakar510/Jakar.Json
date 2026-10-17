// Jakar.Json.RestSharp.Tests
// 10/17/2026

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestSharp;
using RestSharp.Serializers.Json;



namespace Jakar.Json.RestSharp.Tests;


/// <summary> A RestSharp client using Jakar.Json against a Kestrel server using System.Text.Json. </summary>
[TestFixture]
public sealed class RestSharpTests
{
    private LoopbackServer __server = null!;
    private RestClient     __client = null!;


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
                                                                                return Results.Text(await reader.ReadToEndAsync(), "text/plain");
                                                                            });
                                                   });

        __client = new RestClient(new RestClientOptions(__server.Address), configureSerialization: static s => s.UseJakarJson());
    }

    [OneTimeTearDown] public async Task StopAsync()
    {
        __client.Dispose();
        await __server.DisposeAsync();
    }


    // ─── Unit ────────────────────────────────────────────────────────────────

    [Test] public void Serializer_DescribesJson_AndDelegatesContentTypes()
    {
        JakarJsonRestSerializer serializer = new();

        Assert.Multiple(() =>
                        {
                            Assert.That(serializer.DataFormat,            Is.EqualTo(DataFormat.Json));
                            Assert.That(serializer.ContentType,           Is.EqualTo(ContentType.Json));
                            Assert.That(serializer.Fallback,              Is.InstanceOf<SystemTextJsonSerializer>());
                            Assert.That(serializer.AcceptedContentTypes,  Is.EqualTo(serializer.Fallback.AcceptedContentTypes));
                            Assert.That(serializer.Serialize(Samples.Order()), Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(serializer.Serialize(new PlainDto("x")), Does.Not.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(serializer.Serialize((object?)null),  Is.Null);
                        });
    }


    // ─── Integration ─────────────────────────────────────────────────────────

    [Test] public async Task Get_ReadsModelsFromBytes()
    {
        await Assert.MultipleAsync(async () =>
                                   {
                                       Assert.That(await __client.GetAsync<Order>("orders/4"), Is.EqualTo(Samples.Order(4)));
                                       Assert.That(await __client.GetAsync<List<Order>>("orders"), Is.EqualTo(Samples.Orders()));
                                   });
    }

    [Test] public async Task Post_WritesJakarJson_TheServerReadsIt()
    {
        RestRequest echo = new RestRequest("echo", Method.Post).AddJsonBody(Samples.Order());

        await Assert.MultipleAsync(async () =>
                                   {
                                       Assert.That(await __client.PostAsync<Order>(new RestRequest("orders").AddJsonBody(Samples.Order())), Is.EqualTo(Samples.Order(99)));
                                       Assert.That((await __client.ExecuteAsync(echo)).Content,                                            Does.Contain(Samples.JAKAR_SEPARATOR));
                                   });
    }

    [Test] public async Task NonModels_UseTheFallback() => Assert.That(await __client.PostAsync<PlainDto>(new RestRequest("plain").AddJsonBody(new PlainDto("x"))), Is.EqualTo(new PlainDto("x!")));
}
