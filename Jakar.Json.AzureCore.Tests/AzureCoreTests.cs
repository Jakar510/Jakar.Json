// Jakar.Json.AzureCore.Tests
// 10/17/2026

using System.Net;
using System.Text;
using System.Text.Json;
using Azure;
using Azure.Core.Pipeline;
using Azure.Core.Serialization;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;



namespace Jakar.Json.AzureCore.Tests;


[TestFixture]
public sealed class ObjectSerializerTests
{
    private readonly JakarJsonObjectSerializer __serializer = new();


    // ─── Unit ────────────────────────────────────────────────────────────────

    [Test] public async Task Streams_RoundTrip_SyncAndAsync()
    {
        using MemoryStream sync = new();
        __serializer.Serialize(sync, Samples.Order(), typeof(Order), CancellationToken.None);
        sync.Position = 0;

        using MemoryStream async = new();
        await __serializer.SerializeAsync(async, Samples.Orders(), typeof(List<Order>), CancellationToken.None);
        async.Position = 0;

        await Assert.MultipleAsync(async () =>
                                   {
                                       Assert.That(Encoding.UTF8.GetString(sync.ToArray()),                                          Does.Contain(Samples.JAKAR_SEPARATOR));
                                       Assert.That(__serializer.Deserialize(sync, typeof(Order), CancellationToken.None),            Is.EqualTo(Samples.Order()));
                                       Assert.That(await __serializer.DeserializeAsync(async, typeof(List<Order>), CancellationToken.None), Is.EqualTo(Samples.Orders()));
                                   });
    }

    [Test] public void BinaryData_RoundTrips()
    {
        BinaryData data = __serializer.Serialize(Samples.Order());

        Assert.Multiple(() =>
                        {
                            Assert.That(data.ToString(),                         Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(data.ToObject<Order>(__serializer),      Is.EqualTo(Samples.Order()), "BinaryData.ToObject uses the ObjectSerializer");
                        });
    }

    [Test] public void OtherTypes_UseTheFallback()
    {
        BinaryData data = __serializer.Serialize(new PlainDto("x"));

        Assert.Multiple(() =>
                        {
                            Assert.That(__serializer.Fallback,              Is.SameAs(JsonObjectSerializer.Default));
                            Assert.That(data.ToString(),                    Is.EqualTo("{\"Name\":\"x\"}"));
                            Assert.That(data.ToObject<PlainDto>(__serializer), Is.EqualTo(new PlainDto("x")));
                        });
    }

    [Test] public void MemberNames_ComeFromTheFallback()
    {
        JakarJsonObjectSerializer camel = new(new JsonObjectSerializer(JsonSerializerOptions.Web));

        Assert.Multiple(() =>
                        {
                            Assert.That(camel.ConvertMemberName(typeof(Order).GetProperty(nameof(Order.Customer))!), Is.EqualTo("customer"));
                            Assert.That(new JakarJsonObjectSerializer(new NoNamesSerializer()).ConvertMemberName(typeof(Order).GetProperty(nameof(Order.Id))!), Is.Null);
                        });
    }

    [Test] public void Corrupt_Throws()
    {
        using MemoryStream stream = new("{\"id\":"u8.ToArray());
        Assert.Throws<JsonReadException>(() => __serializer.Deserialize(stream, typeof(Order), CancellationToken.None));
    }



    internal sealed class NoNamesSerializer : ObjectSerializer
    {
        public override void            Serialize( Stream        stream, object? value, Type inputType, CancellationToken cancellationToken ) => throw new NotSupportedException();
        public override ValueTask       SerializeAsync( Stream   stream, object? value, Type inputType, CancellationToken cancellationToken ) => throw new NotSupportedException();
        public override object?         Deserialize( Stream      stream, Type returnType, CancellationToken cancellationToken ) => throw new NotSupportedException();
        public override ValueTask<object?> DeserializeAsync( Stream stream, Type returnType, CancellationToken cancellationToken ) => throw new NotSupportedException();
    }
}



/// <summary> An Azure SDK client (Azure AI Search) using the serializer, over a stubbed HTTP transport. </summary>
[TestFixture]
public sealed class SearchClientTests
{
    [Test] public async Task GetDocument_ReadsWithJakarJson()
    {
        SearchClient client = Client(new JakarJsonObjectSerializer(new ObjectSerializerTests.NoNamesSerializer()), static _ => Json(HttpStatusCode.OK, Samples.Order().ToJson()));

        Response<Order> response = await client.GetDocumentAsync<Order>("7");
        Assert.That(response.Value, Is.EqualTo(Samples.Order()));
    }

    [Test] public async Task UploadDocuments_WritesWithJakarJson()
    {
        string? body = null;

        // The fallback throws: the documents must be written by Jakar.Json.
        SearchClient client = Client(new JakarJsonObjectSerializer(new ObjectSerializerTests.NoNamesSerializer()), request =>
                                     {
                                         body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                                         return Json(HttpStatusCode.OK, """{"value":[{"key":"7","status":true,"errorMessage":null,"statusCode":201}]}""");
                                     });

        Response<IndexDocumentsResult> result = await client.UploadDocumentsAsync(new[] { Samples.Order() });

        Assert.Multiple(() =>
                        {
                            Assert.That(result.Value.Results.Single().Succeeded, Is.True);
                            // Search parses each serialized document and rewrites it inside its batch envelope (re-escaping text), so check the values.
                            using JsonDocument document = JsonDocument.Parse(body!);
                            JsonElement        uploaded = document.RootElement.GetProperty("value")[0];
                            Assert.That(uploaded.GetProperty("@search.action").GetString(), Is.EqualTo("upload"));
                            Assert.That(uploaded.GetProperty("customer").GetString(),       Is.EqualTo("Zoë"));
                            Assert.That(uploaded.GetProperty("lines")[0].GetProperty("sku").GetString(), Is.EqualTo("A-1"));
                        });
    }


    private static SearchClient Client( ObjectSerializer serializer, Func<HttpRequestMessage, HttpResponseMessage> respond )
    {
        SearchClientOptions options = new()
                                      {
                                          Serializer = serializer,
                                          Transport  = new HttpClientTransport(new StubHandler(respond)),
                                          Retry      = { MaxRetries = 0 }
                                      };

        return new SearchClient(new Uri("https://search.test"), "orders", new AzureKeyCredential("key"), options);
    }

    private static HttpResponseMessage Json( HttpStatusCode status, string json ) => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };



    private sealed class StubHandler( Func<HttpRequestMessage, HttpResponseMessage> respond ) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken ) => Task.FromResult(respond(request));
    }
}
