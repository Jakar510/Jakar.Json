// Jakar.Json.Refit.Tests
// 10/17/2026

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Refit;



namespace Jakar.Json.Refit.Tests;


public interface IOrdersApi
{
    [Get("/orders/{id}")]   Task<Order>               GetAsync( int id );
    [Get("/orders")]        Task<List<Order>>         ListAsync();
    [Post("/orders")]       Task<Order>               CreateAsync( [Body] Order order );
    [Get("/orders/{id}")]   Task<IApiResponse<Order>> TryGetAsync( int id );
    [Post("/plain")]        Task<PlainDto>            PlainAsync( [Body] PlainDto dto );
    [Post("/echo")]         Task<string>              EchoAsync( [Body] Order order );
}



[TestFixture]
public sealed class RefitTests
{
    private LoopbackServer __server = null!;
    private HttpClient     __client = null!;
    private IOrdersApi     __api    = null!;


    [OneTimeSetUp] public async Task StartAsync()
    {
        __server = await LoopbackServer.StartAsync(static app =>
                                                   {
                                                       app.MapGet("/orders/{id:int}", static ( int id ) => id > 0 ? Results.Ok(Samples.Order(id)) : Results.NotFound());
                                                       app.MapGet("/orders",          static () => TypedResults.Ok(Samples.Orders()));
                                                       app.MapPost("/orders",         static ( [FromBody] Order order ) => TypedResults.Ok(order with { Id = 99 }));
                                                       app.MapPost("/plain",          static ( [FromBody] PlainDto dto ) => TypedResults.Ok(dto with { Name = dto.Name + "!" }));
                                                       app.MapPost("/echo", static async ( HttpRequest request ) =>
                                                                            {
                                                                                using StreamReader reader = new(request.Body);
                                                                                return Results.Text(await reader.ReadToEndAsync());
                                                                            });
                                                   });

        __client = __server.CreateClient();
        __api    = RestService.For<IOrdersApi>(__client, new RefitSettings().UseJakarJson());
    }

    [OneTimeTearDown] public async Task StopAsync()
    {
        __client.Dispose();
        await __server.DisposeAsync();
    }


    // ─── Unit ────────────────────────────────────────────────────────────────

    [Test] public void UseJakarJson_WrapsTheExistingSerializer_Once()
    {
        RefitSettings settings = new RefitSettings().UseJakarJson().UseJakarJson();

        Assert.Multiple(() =>
                        {
                            Assert.That(settings.ContentSerializer,                                       Is.InstanceOf<JakarJsonContentSerializer>());
                            Assert.That(((JakarJsonContentSerializer)settings.ContentSerializer).Fallback, Is.InstanceOf<SystemTextJsonContentSerializer>());
                        });
    }

    [Test] public void ToHttpContent_ModelsAreJakarJson_OthersFallBack()
    {
        JakarJsonContentSerializer serializer = new();

        Assert.Multiple(() =>
                        {
                            Assert.That(serializer.ToHttpContent(Samples.Order()),      Is.InstanceOf<Jakar.Json.Http.JakarJsonContent>());
                            Assert.That(serializer.ToHttpContent(new PlainDto("x")),    Is.Not.InstanceOf<Jakar.Json.Http.JakarJsonContent>());
                        });
    }


    // ─── Integration: a Refit client against a System.Text.Json server ───────

    [Test] public async Task Get_And_List()
    {
        await Assert.MultipleAsync(async () =>
                        {
                            Assert.That(await __api.GetAsync(3),  Is.EqualTo(Samples.Order(3)));
                            Assert.That(await __api.ListAsync(),  Is.EqualTo(Samples.Orders()));
                        });
    }

    [Test] public async Task Post_WritesJakarJson_TheServerReadsIt()
    {
        await Assert.MultipleAsync(async () =>
                        {
                            Assert.That(await __api.CreateAsync(Samples.Order()),   Is.EqualTo(Samples.Order(99)));
                            Assert.That(await __api.EchoAsync(Samples.Order()),     Does.Contain(Samples.JAKAR_SEPARATOR));
                        });
    }

    [Test] public async Task NonModels_UseTheFallback() => Assert.That(await __api.PlainAsync(new PlainDto("x")), Is.EqualTo(new PlainDto("x!")));

    [Test] public async Task Errors_AreRefitErrors()
    {
        using IApiResponse<Order> response = await __api.TryGetAsync(-1);

        await Assert.MultipleAsync(async () =>
                        {
                            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                            Assert.That(response.IsSuccessStatusCode, Is.False);
                            await Assert.ThrowsAsync<ApiException>(() => __api.GetAsync(-1));
                        });
    }
}
