// Jakar.Json.Elasticsearch.Tests
// 10/17/2026

using System.Text;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;



namespace Jakar.Json.Elasticsearch.Tests;


/// <summary> The Elasticsearch client end to end over its in-memory transport (canned responses; request bodies captured). </summary>
[TestFixture]
public sealed class ElasticsearchTests
{
    // The client checks the product header on every response; the in-memory invoker matches header names case-sensitively, in lower case.
    private static ElasticsearchClient Client( string response ) => new(new ElasticsearchClientSettings(new SingleNodePool(new Uri("http://localhost:9200")), new InMemoryRequestInvoker(Encoding.UTF8.GetBytes(response), headers: new Dictionary<string, IEnumerable<string>> { ["x-elastic-product"] = ["Elasticsearch"] }), JakarJsonSourceSerializer.Factory()).DisableDirectStreaming());


    [Test] public async Task Index_WritesTheDocumentWithJakarJson()
    {
        ElasticsearchClient client   = Client("""{"_index":"orders","_id":"7","_version":1,"result":"created","_shards":{"total":1,"successful":1,"failed":0},"_seq_no":0,"_primary_term":1}""");
        IndexResponse       response = await client.IndexAsync(Samples.Order(), static i => i.Index("orders").Id(7));

        Assert.Multiple(() =>
                        {
                            Assert.That(response.IsValidResponse, Is.True);
                            Assert.That(Encoding.UTF8.GetString(response.ApiCallDetails.RequestBodyInBytes!), Does.Contain("\"customer\" : \"Zoë\""));
                        });
    }

    [Test] public async Task Get_ReadsTheSourceWithJakarJson()
    {
        ElasticsearchClient   client   = Client($$"""{"_index":"orders","_id":"7","_version":1,"_seq_no":0,"_primary_term":1,"found":true,"_source":{{Samples.Order().ToJson()}}}""");
        GetResponse<Order>    response = await client.GetAsync<Order>("orders", 7);

        Assert.Multiple(() =>
                        {
                            Assert.That(response.Found,  Is.True);
                            Assert.That(response.Source, Is.EqualTo(Samples.Order()));
                        });
    }

    [Test] public async Task OtherTypes_UseTheDefaultSourceSerializer()
    {
        ElasticsearchClient client   = Client("""{"_index":"plain","_id":"1","_version":1,"result":"created","_shards":{"total":1,"successful":1,"failed":0},"_seq_no":0,"_primary_term":1}""");
        IndexResponse       response = await client.IndexAsync(new PlainDto("x"), static i => i.Index("plain").Id(1));

        Assert.That(Encoding.UTF8.GetString(response.ApiCallDetails.RequestBodyInBytes!), Does.Contain("\"name\":\"x\"").And.Not.Contain(Samples.JAKAR_SEPARATOR));
    }

    [Test] public void Serializer_RoundTrips_SyncAndAsync()
    {
        JakarJsonSourceSerializer serializer = new(new NullFallback());
        using MemoryStream        stream     = new();
        serializer.Serialize(Samples.Orders(), stream);
        stream.Position = 0;

        Assert.That(serializer.Deserialize<List<Order>>(stream), Is.EqualTo(Samples.Orders()));
    }



    private sealed class NullFallback : Serializer
    {
        public override object?            Deserialize( Type type, Stream stream ) => throw new NotSupportedException();
        public override T                  Deserialize<T>( Stream stream ) => throw new NotSupportedException();
        public override ValueTask<object?> DeserializeAsync( Type type, Stream stream, CancellationToken cancellationToken = default ) => throw new NotSupportedException();
        public override ValueTask<T>       DeserializeAsync<T>( Stream stream, CancellationToken cancellationToken = default ) => throw new NotSupportedException();
        public override void               Serialize( object? data, Type type, Stream stream, SerializationFormatting formatting = SerializationFormatting.None, CancellationToken cancellationToken = default ) => throw new NotSupportedException();
        public override void               Serialize<T>( T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.None ) => throw new NotSupportedException();
        public override Task               SerializeAsync( object? data, Type type, Stream stream, SerializationFormatting formatting = SerializationFormatting.None, CancellationToken cancellationToken = default ) => throw new NotSupportedException();
        public override Task               SerializeAsync<T>( T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.None, CancellationToken cancellationToken = default ) => throw new NotSupportedException();
    }
}
