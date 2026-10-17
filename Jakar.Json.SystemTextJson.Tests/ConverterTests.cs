// Jakar.Json.SystemTextJson.Tests
// 10/17/2026

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;



namespace Jakar.Json.SystemTextJson.Tests;


[TestFixture]
public sealed class ConverterTests
{
    private static JsonSerializerOptions Options() => new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver() }.AddJakarJson();


    // ─── Unit: the converter ─────────────────────────────────────────────────

    [Test] public void Model_RoundTrips_WrittenByJakarJson()
    {
        string json = JsonSerializer.Serialize(Samples.Order(), Options());

        Assert.Multiple(() =>
                        {
                            Assert.That(json,                                                   Does.Contain(Samples.JAKAR_SEPARATOR), "written by Jakar.Json");
                            Assert.That(JsonSerializer.Deserialize<Order>(json, Options()),     Is.EqualTo(Samples.Order()));
                        });
    }

    [Test] public void CollectionsAndStructs_RoundTrip()
    {
        JsonSerializerOptions options = Options();

        Assert.Multiple(() =>
                        {
                            Assert.That(JsonSerializer.Deserialize<List<Order>>(JsonSerializer.Serialize(Samples.Orders(), options), options), Is.EqualTo(Samples.Orders()));
                            Assert.That(JsonSerializer.Deserialize<Order[]>(JsonSerializer.Serialize(Samples.Orders().ToArray(), options), options), Is.EqualTo(Samples.Orders().ToArray()));
                            Assert.That(JsonSerializer.Deserialize<IReadOnlyList<Order>>(JsonSerializer.Serialize<IReadOnlyList<Order>>(Samples.Orders(), options), options), Is.EqualTo(Samples.Orders()));
                            Assert.That(JsonSerializer.Deserialize<Point>(JsonSerializer.Serialize(new Point { X = 1, Y = -2 }, options), options), Is.EqualTo(new Point { X = 1, Y = -2 }));
                        });
    }

    [Test] public void ModelsNestedInPlainTypes_UseJakarJson_RestUsesSystemTextJson()
    {
        JsonSerializerOptions options  = Options();
        OrderBatch              envelope = new() { Kind = "batch", Order = Samples.Order(), Orders = Samples.Orders() };

        string   json = JsonSerializer.Serialize(envelope, options);
        OrderBatch back = JsonSerializer.Deserialize<OrderBatch>(json, options)!;

        Assert.Multiple(() =>
                        {
                            Assert.That(json,        Does.StartWith("{\"kind\":\"batch\""), "the envelope is System.Text.Json's (web naming, no spacing)");
                            Assert.That(json,        Does.Contain("\"customer\" : \"Zoë\""),  "the models are Jakar.Json's");
                            Assert.That(back.Kind,   Is.EqualTo("batch"));
                            Assert.That(back.Order,  Is.EqualTo(Samples.Order()));
                            Assert.That(back.Orders, Is.EqualTo(Samples.Orders()));
                        });
    }

    [Test] public void Null_ReferenceIsNull_StructThrows()
    {
        JsonSerializerOptions options = Options();

        Assert.Multiple(() =>
                        {
                            Assert.That(JsonSerializer.Serialize<Order?>(null, options),                                Is.EqualTo("null"));
                            Assert.That(JsonSerializer.Deserialize<Order?>("null", options),                            Is.Null);
                            Assert.That(JsonSerializer.Deserialize<OrderBatch>("{\"order\":null}", options)!.Order,       Is.Null);
                            Assert.Catch<JsonException>(() => JsonSerializer.Deserialize<Point>("null", options));
                        });
    }

    [Test] public void Malformed_ThrowsJsonException()
    {
        JsonSerializerOptions options = Options();

        Assert.Multiple(() =>
                        {
                            Assert.Catch<JsonException>(() => JsonSerializer.Deserialize<Order>("{\"id\":\"not a number\"}", options));
                            Assert.Catch<JsonException>(() => JsonSerializer.Deserialize<Order>("{\"id\":1", options));
                            Assert.Catch<JsonException>(() => JsonSerializer.Deserialize<Order>("[1]", options));
                        });
    }

    [Test] public async Task Stream_MultiSegmentBuffers_RoundTrip()
    {
        // A tiny buffer makes System.Text.Json read the value across many segments.
        JsonSerializerOptions options = new JsonSerializerOptions { DefaultBufferSize = 16 }.AddJakarJson();
        List<Order>           orders  = Enumerable.Range(1, 50).Select(Samples.Order).ToList();

        using MemoryStream stream = new();
        await JsonSerializer.SerializeAsync(stream, new OrderBatch { Kind = "big", Orders = orders }, options);
        stream.Position = 0;

        OrderBatch? back = await JsonSerializer.DeserializeAsync<OrderBatch>(stream, options);
        Assert.That(back!.Orders, Is.EqualTo(orders));
    }

    [Test] public void Factory_ConvertsOnlyModelsAndTheirCollections()
    {
        JakarJsonConverterFactory factory = new();

        Assert.Multiple(() =>
                        {
                            foreach ( Type type in new[] { typeof(Order), typeof(Point), typeof(List<Order>), typeof(Order[]), typeof(IEnumerable<Order>) } ) { Assert.That(factory.CanConvert(type), Is.True, type.ToString()); }

                            foreach ( Type type in new[] { typeof(PlainDto), typeof(string), typeof(int), typeof(OrderBatch), typeof(Dictionary<string, Order>) } ) { Assert.That(factory.CanConvert(type), Is.False, type.ToString()); }

                            Assert.That(factory.CreateConverter(typeof(Order), JsonSerializerOptions.Default), Is.InstanceOf<JakarJsonConverter<Order>>());
                        });
    }

    [Test] public void Converter_ForUnsupportedType_Throws() => Assert.Throws<NotSupportedException>(() => _ = new JakarJsonConverter<PlainDto>());

    [Test] public void AddJakarJson_IsIdempotent_AndGoesFirst()
    {
        JsonSerializerOptions options = new() { Converters = { new JsonStringEnumConverter() } };
        options.AddJakarJson().AddJakarJson(static f => f.Add<Order>());

        Assert.Multiple(() =>
                        {
                            Assert.That(options.Converters.OfType<JakarJsonConverterFactory>().Count(), Is.EqualTo(1));
                            Assert.That(options.Converters[0],                                        Is.InstanceOf<JakarJsonConverterFactory>());
                        });
    }

    [Test] public void SingleTypeConverter_Works()
    {
        JsonSerializerOptions options = new() { Converters = { new JakarJsonConverter<Order>() } };
        Assert.That(JsonSerializer.Deserialize<Order>(JsonSerializer.Serialize(Samples.Order(), options), options), Is.EqualTo(Samples.Order()));
    }

    [Test] public void SourceGeneratedContext_WithConverter_RoundTrips()
    {
        // The Native AOT setup: a source-generated context, with the models added to the factory explicitly.
        JsonSerializerOptions options = new(ConverterTestsContext.Default.Options);
        options.AddJakarJson(static f => f.Add<Order>().Add<List<Order>>());

        string json = JsonSerializer.Serialize(new OrderBatch { Kind = "x", Order = Samples.Order(), Orders = Samples.Orders() }, options.GetTypeInfo(typeof(OrderBatch)));

        Assert.Multiple(() =>
                        {
                            Assert.That(json,                                                                                                  Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(((OrderBatch)JsonSerializer.Deserialize(json, options.GetTypeInfo(typeof(OrderBatch)))!).Orders,         Is.EqualTo(Samples.Orders()));
                        });
    }


    // ─── Integration: hosts configured through JsonSerializerOptions ─────────

    [Test] public async Task HttpClient_SystemNetHttpJson_UsesJakarJson()
    {
        string? posted = null;

        using HttpClient client = new(new DelegatingStub(async request =>
                                                         {
                                                             posted = await request.Content!.ReadAsStringAsync();
                                                             return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(posted, Encoding.UTF8, "application/json") };
                                                         })) { BaseAddress = new Uri("https://api.test/") };

        HttpResponseMessage response = await client.PostAsJsonAsync("orders", Samples.Order(), Options());
        Order?              echoed   = await response.Content.ReadFromJsonAsync<Order>(Options());

        Assert.Multiple(() =>
                        {
                            Assert.That(posted, Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(echoed, Is.EqualTo(Samples.Order()));
                        });
    }

    [Test] public async Task MicrosoftExtensionsAI_FunctionArguments_UseJakarJson()
    {
        Order? received = null;

        AIFunction function = AIFunctionFactory.Create(( Order order ) =>
                                                       {
                                                           received = order;
                                                           return order.Lines.Count;
                                                       },
                                                       serializerOptions: Options());

        // A model returns tool arguments as JSON.
        JsonElement argument = JsonDocument.Parse(JsonSerializer.Serialize(Samples.Order(), Options())).RootElement;
        object?     result   = await function.InvokeAsync(new AIFunctionArguments { ["order"] = argument });

        Assert.Multiple(() =>
                        {
                            Assert.That(received,             Is.EqualTo(Samples.Order()));
                            Assert.That(result?.ToString(),   Is.EqualTo("1"));
                        });
    }



    private sealed class DelegatingStub( Func<HttpRequestMessage, Task<HttpResponseMessage>> respond ) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken ) => respond(request);
    }
}



[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(OrderBatch))]
internal sealed partial class ConverterTestsContext : JsonSerializerContext;
