// Jakar.Json.Http.Tests
// 10/17/2026

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;



namespace Jakar.Json.Http.Tests;


[TestFixture]
public sealed class JakarJsonContentTests
{
    // ─── Unit ────────────────────────────────────────────────────────────────

    [Test] public async Task Content_WritesJakarJson_WithJsonMediaType()
    {
        using JakarJsonContent content = JakarJsonContent.Create(Samples.Order());
        long?                  length  = content.Headers.ContentLength; // before reading: reading buffers the content
        string                 body    = await content.ReadAsStringAsync();

        Assert.Multiple(() =>
                        {
                            Assert.That(content.Headers.ContentType!.MediaType, Is.EqualTo("application/json"));
                            Assert.That(content.Headers.ContentType.CharSet,    Is.EqualTo("utf-8"));
                            Assert.That(length,                                 Is.Null, "length isn't known in advance, like JsonContent");
                            Assert.That(content.Value,                          Is.EqualTo(Samples.Order()));
                            Assert.That(content.ObjectType,                     Is.EqualTo(typeof(Order)));
                            Assert.That(body,                                   Does.Contain("\"customer\" : \"Zoë\""));
                        });
    }

    [Test] public async Task Content_CustomMediaType_AndNull()
    {
        using JakarJsonContent content = JakarJsonContent.Create<Order?>(null, new MediaTypeHeaderValue("application/vnd.orders+json"));

        await Assert.MultipleAsync(async () =>
                        {
                            Assert.That(content.Headers.ContentType!.MediaType, Is.EqualTo("application/vnd.orders+json"));
                            Assert.That(await content.ReadAsStringAsync(),      Is.EqualTo("null"));
                        });
    }

    [Test] public void Content_SyncSerialization_Works()
    {
        using JakarJsonContent content = JakarJsonContent.Create(Samples.Orders());
        using MemoryStream     stream  = new();
        content.CopyTo(stream, null, CancellationToken.None);

        Assert.That(JsonSerializer.Deserialize<List<JsonElement>>(stream.ToArray()), Has.Count.EqualTo(2));
    }

    [Test] public void Content_ForUnsupportedType_Throws() => Assert.Throws<NotSupportedException>(() => JakarJsonContent.Create(new PlainDto("x")));


    [Test] public async Task Read_Utf8_AndOtherCharsets()
    {
        string json = Samples.Order().ToJson();

        using StringContent utf8    = new(json, Encoding.UTF8, "application/json");
        using StringContent latin1  = new(json.Replace("Zoë", "Zoe"), Encoding.Latin1, "application/json");
        using StringContent unicode = new(json, Encoding.Unicode, "application/json");

        await Assert.MultipleAsync(async () =>
                        {
                            Assert.That(await utf8.ReadFromJakarJsonAsync<Order>(),    Is.EqualTo(Samples.Order()));
                            Assert.That(await latin1.ReadFromJakarJsonAsync<Order>(),  Is.EqualTo(Samples.Order() with { Customer = "Zoe" }));
                            Assert.That(await unicode.ReadFromJakarJsonAsync<Order>(), Is.EqualTo(Samples.Order()), "UTF-16 is transcoded");
                        });
    }

    [Test] public async Task Read_NullBody_IsDefault_AndMalformedThrows()
    {
        using StringContent nullBody  = new("null", Encoding.UTF8, "application/json");
        using StringContent malformed = new("{\"id\":", Encoding.UTF8, "application/json");

        await Assert.MultipleAsync(async () =>
                        {
                            Assert.That(await nullBody.ReadFromJakarJsonAsync<Order>(), Is.Null);
                            await Assert.ThrowsAsync<JsonReadException>(() => malformed.ReadFromJakarJsonAsync<Order>());
                        });
    }
}



/// <summary> End to end over Kestrel: a server using System.Text.Json (its defaults) talking to a client using Jakar.Json, and back. </summary>
[TestFixture]
public sealed class HttpClientTests
{
    private LoopbackServer __server = null!;
    private HttpClient     __client = null!;


    [OneTimeSetUp] public async Task StartAsync()
    {
        __server = await LoopbackServer.StartAsync(static app =>
                                                   {
                                                       app.MapGet("/orders/{id:int}", static ( int id ) => TypedResults.Ok(Samples.Order(id)));
                                                       app.MapGet("/orders",          static () => TypedResults.Ok(Enumerable.Range(1, 500).Select(Samples.Order).ToList()));
                                                       app.MapGet("/missing",         static () => TypedResults.NotFound());
                                                       app.MapPost("/orders",         static ( [Microsoft.AspNetCore.Mvc.FromBody] Order order ) => TypedResults.Created($"/orders/{order.Id}", order));
                                                       app.MapPut("/orders/{id:int}", static ( int id, [Microsoft.AspNetCore.Mvc.FromBody] Order order ) => TypedResults.Ok(order with { Id = id }));
                                                       app.MapPatch("/orders",        static ( [Microsoft.AspNetCore.Mvc.FromBody] Order order ) => TypedResults.Ok(order with { Customer = order.Customer + "!" }));
                                                       app.MapDelete("/orders/{id:int}", static ( int id ) => TypedResults.Ok(Samples.Order(id)));
                                                       app.MapPost("/echo", static async ( HttpRequest request ) =>
                                                                            {
                                                                                using StreamReader reader = new(request.Body);
                                                                                return Results.Text(await reader.ReadToEndAsync(), request.ContentType);
                                                                            });
                                                   });

        __client = __server.CreateClient();
    }

    [OneTimeTearDown] public async Task StopAsync()
    {
        __client.Dispose();
        await __server.DisposeAsync();
    }


    [Test] public async Task Get_ReadsServerJson()
    {
        await Assert.MultipleAsync(async () =>
                        {
                            Assert.That(await __client.GetFromJakarJsonAsync<Order>("orders/9"),                     Is.EqualTo(Samples.Order(9)));
                            Assert.That(await __client.GetFromJakarJsonAsync<Order>(new Uri("orders/9", UriKind.Relative)), Is.EqualTo(Samples.Order(9)));
                        });
    }

    [Test] public async Task Get_LargeStreamedList() => Assert.That(await __client.GetFromJakarJsonAsync<List<Order>>("orders"), Has.Count.EqualTo(500).And.All.Property(nameof(Order.Customer)).EqualTo("Zoë"));

    [Test] public async Task Get_Failure_Throws()
    {
        HttpRequestException? exception = await Assert.ThrowsAsync<HttpRequestException>(() => __client.GetFromJakarJsonAsync<Order>("missing"));
        Assert.That(exception!.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test] public async Task Post_Put_Patch_Delete_RoundTripWithTheServer()
    {
        using HttpResponseMessage post  = await __client.PostAsJakarJsonAsync("orders", Samples.Order(3));
        using HttpResponseMessage put   = await __client.PutAsJakarJsonAsync("orders/4", Samples.Order(3));
        using HttpResponseMessage patch = await __client.PatchAsJakarJsonAsync("orders", Samples.Order(5));

        await Assert.MultipleAsync(async () =>
                        {
                            Assert.That(post.StatusCode,                                         Is.EqualTo(HttpStatusCode.Created));
                            Assert.That(await post.Content.ReadFromJakarJsonAsync<Order>(),      Is.EqualTo(Samples.Order(3)), "the server (System.Text.Json) read what Jakar.Json wrote");
                            Assert.That(await put.Content.ReadFromJakarJsonAsync<Order>(),       Is.EqualTo(Samples.Order(4)));
                            Assert.That((await patch.Content.ReadFromJakarJsonAsync<Order>())!.Customer, Is.EqualTo("Zoë!"));
                            Assert.That(await __client.DeleteFromJakarJsonAsync<Order>("orders/6"), Is.EqualTo(Samples.Order(6)));
                        });
    }

    [Test] public async Task Post_SendsJakarJsonBody()
    {
        using HttpResponseMessage response = await __client.PostAsJakarJsonAsync("echo", Samples.Order());
        string                    body     = await response.Content.ReadAsStringAsync();

        Assert.Multiple(() =>
                        {
                            Assert.That(response.Content.Headers.ContentType!.MediaType, Is.EqualTo("application/json"));
                            Assert.That(body,                                           Does.Contain(Samples.JAKAR_SEPARATOR));
                        });
    }

    [Test] public async Task Interop_SystemNetHttpJson_ReadsJakarJson()
    {
        using HttpResponseMessage response = await __client.PostAsJakarJsonAsync("echo", Samples.Order());
        Assert.That(await response.Content.ReadFromJsonAsync<Order>(JsonSerializerOptions.Web), Is.EqualTo(Samples.Order()));
    }
}
