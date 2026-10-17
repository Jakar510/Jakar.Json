// Jakar.Json.Cosmos.Tests
// 10/17/2026

using System.Text;
using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;



namespace Jakar.Json.Cosmos.Tests;


[TestFixture]
public sealed class CosmosTests
{
    // The Cosmos DB emulator's well-known, public account key (not a secret).
    private const string EMULATOR = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    private readonly JakarJsonCosmosSerializer __serializer = new();


    // ─── Unit ────────────────────────────────────────────────────────────────

    [Test] public void Items_RoundTrip()
    {
        using Stream stream = __serializer.ToStream(Samples.Order());
        string       json   = new StreamReader(stream, leaveOpen: true).ReadToEnd();
        stream.Position = 0;

        Assert.Multiple(() =>
                        {
                            Assert.That(json,                                       Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(__serializer.FromStream<Order>(stream),      Is.EqualTo(Samples.Order()));
                        });
    }

    [Test] public void Collections_AndNonMemoryStreams()
    {
        byte[]             bytes  = Encoding.UTF8.GetBytes(JsonCodec.ToJsonArray(Samples.Orders()));
        using BufferedStream stream = new(new MemoryStream(bytes), 7);

        Assert.That(__serializer.FromStream<List<Order>>(stream), Is.EqualTo(Samples.Orders()));
    }

    [Test] public void FromStream_DisposesTheStream()
    {
        TrackingStream stream = new(Encoding.UTF8.GetBytes(Samples.Order().ToJson()));
        _ = __serializer.FromStream<Order>(stream);
        Assert.That(stream.Disposed, Is.True, "Cosmos expects the serializer to dispose the response stream");
    }

    [Test] public void Streams_PassThrough()
    {
        MemoryStream stream = new([1, 2, 3]);
        Assert.That(__serializer.FromStream<Stream>(stream), Is.SameAs(stream));
    }

    [Test] public void OtherTypes_UseTheFallback()
    {
        using Stream stream = __serializer.ToStream(new PlainDto("x"));

        Assert.Multiple(() =>
                        {
                            Assert.That(new StreamReader(stream).ReadToEnd(),                         Is.EqualTo("{\"name\":\"x\"}"), "System.Text.Json, camelCase");
                            Assert.That(__serializer.FromStream<PlainDto>(new MemoryStream("{\"name\":\"y\"}"u8.ToArray())), Is.EqualTo(new PlainDto("y")));
                        });
    }

    [Test] public void MemberNames_ForLinq_ComeFromTheFallback()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(__serializer.SerializeMemberName(typeof(Order).GetProperty(nameof(Order.Customer))!), Is.EqualTo("customer"));
                            Assert.That(new JakarJsonCosmosSerializer(new SystemTextJsonCosmosSerializer(new JsonSerializerOptions())).SerializeMemberName(typeof(Order).GetProperty(nameof(Order.Customer))!), Is.EqualTo("Customer"));
                        });
    }


    // ─── Integration: the Cosmos client's LINQ translation (no network) ──────

    [Test] public void Linq_TranslatesWithTheModelsNames()
    {
        using CosmosClient client    = new(EMULATOR, new CosmosClientOptions { Serializer = __serializer });
        Container          container = client.GetContainer("shop", "orders");

        QueryDefinition query = container.GetItemLinqQueryable<Order>().Where(static o => o.Customer == "Zoë" && o.Total > 10).OrderBy(static o => o.Id).ToQueryDefinition();

        Assert.That(query.QueryText, Does.Contain("root[\"customer\"]").And.Contain("root[\"total\"]").And.Contain("root[\"id\"]"));
    }

    [Test] public void Client_AcceptsTheSerializer()
    {
        using CosmosClient client = new(EMULATOR, new CosmosClientOptions { Serializer = __serializer });
        Assert.That(client.ClientOptions.Serializer, Is.SameAs(__serializer));
    }



    private sealed class TrackingStream( byte[] data ) : MemoryStream(data)
    {
        public bool Disposed { get; private set; }

        protected override void Dispose( bool disposing )
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
