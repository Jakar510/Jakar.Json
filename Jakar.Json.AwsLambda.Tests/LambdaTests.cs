// Jakar.Json.AwsLambda.Tests
// 10/17/2026

using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.TestUtilities;



namespace Jakar.Json.AwsLambda.Tests;


[TestFixture]
public sealed class LambdaTests
{
    private static readonly JakarJsonLambdaSerializer __serializer = new JakarJsonLambdaSerializer<SourceGeneratorLambdaJsonSerializer<LambdaTestsContext>>();


    // ─── Unit ────────────────────────────────────────────────────────────────

    [Test] public void Models_RoundTrip()
    {
        using MemoryStream response = new();
        __serializer.Serialize(Samples.Order(), response);
        response.Position = 0;

        Assert.Multiple(() =>
                        {
                            Assert.That(Encoding.UTF8.GetString(response.ToArray()), Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(__serializer.Deserialize<Order>(response),   Is.EqualTo(Samples.Order()));
                        });
    }

    [Test] public void LargeRequests_AreReadFully()
    {
        List<Order>        orders = Enumerable.Range(1, 2000).Select(Samples.Order).ToList();
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(JsonCodec.ToJsonArray(orders)));

        Assert.That(__serializer.Deserialize<List<Order>>(stream), Is.EqualTo(orders));
    }

    [Test] public void OtherTypes_UseTheFallback()
    {
        using MemoryStream response = new();
        __serializer.Serialize(new PlainDto("x"), response);
        response.Position = 0;

        Assert.Multiple(() =>
                        {
                            Assert.That(Encoding.UTF8.GetString(response.ToArray()), Does.Not.Contain(Samples.JAKAR_SEPARATOR), "System.Text.Json (source-generated)");
                            Assert.That(__serializer.Deserialize<PlainDto>(response), Is.EqualTo(new PlainDto("x")));
                        });
    }

    [Test] public void Corrupt_Throws()
    {
        using MemoryStream stream = new("{\"id\":"u8.ToArray());
        Assert.Throws<JsonReadException>(() => __serializer.Deserialize<Order>(stream));
    }

    [Test] public void Fallback_IsRequired() => Assert.Throws<ArgumentNullException>(() => _ = new JakarJsonLambdaSerializer(null!));


    // ─── Integration: the Lambda runtime's handler pipeline ──────────────────

    [Test] public async Task RuntimeHandler_ReadsTheRequestAndWritesTheResponse()
    {
        using HandlerWrapper wrapper = HandlerWrapper.GetHandlerWrapper<Order, Order>(static order => Task.FromResult(order with { Customer = order.Customer + "!" }), __serializer);

        using MemoryStream request  = new(Encoding.UTF8.GetBytes(Samples.Order().ToJson()));
        using InvocationRequest invocation = CreateInvocation(request, new TestLambdaContext());

        InvocationResponse response = await wrapper.Handler(invocation);
        using StreamReader reader   = new(response.OutputStream);
        string             body     = await reader.ReadToEndAsync();

        Assert.Multiple(() =>
                        {
                            Assert.That(body,                                 Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(Order.FromJson(body).Customer,        Is.EqualTo("Zoë!"));
                        });
    }


    /// <summary> The runtime creates invocations itself (the constructor is internal); tests build one the same way. </summary>
    private static InvocationRequest CreateInvocation( Stream input, ILambdaContext context )
    {
        ConstructorInfo constructor = typeof(InvocationRequest).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).OrderByDescending(static c => c.GetParameters().Length).First();
        object?[]       arguments   = constructor.GetParameters().Select(p => p.ParameterType == typeof(Stream) ? input : p.ParameterType == typeof(ILambdaContext) ? context : (object?)null).ToArray();
        InvocationRequest request     = (InvocationRequest)constructor.Invoke(arguments);
        if ( request.InputStream is null ) { typeof(InvocationRequest).GetProperty(nameof(InvocationRequest.InputStream))!.SetValue(request, input); }
        if ( request.LambdaContext is null ) { typeof(InvocationRequest).GetProperty(nameof(InvocationRequest.LambdaContext))!.SetValue(request, context); }
        return request;
    }
}



[JsonSerializable(typeof(PlainDto))]
public sealed partial class LambdaTestsContext : JsonSerializerContext;
