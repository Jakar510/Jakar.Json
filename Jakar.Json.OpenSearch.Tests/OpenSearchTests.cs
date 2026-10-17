// Jakar.Json.OpenSearch.Tests
// 10/17/2026

using System.Text;
using OpenSearch.Client;
using OpenSearch.Net;



namespace Jakar.Json.OpenSearch.Tests;


/// <summary> The OpenSearch client end to end over its in-memory connection (canned responses; request bodies captured). </summary>
[TestFixture]
public sealed class OpenSearchTests
{
    private static OpenSearchClient Client( string response ) => new(new ConnectionSettings(new SingleNodeConnectionPool(new Uri("http://localhost:9200")), new InMemoryConnection(Encoding.UTF8.GetBytes(response)), JakarJsonOpenSearchSerializer.Factory).DisableDirectStreaming().DefaultIndex("orders"));


    [Test] public async Task Index_WritesTheDocumentWithJakarJson()
    {
        OpenSearchClient client   = Client("""{"_index":"orders","_id":"7","_version":1,"result":"created","_shards":{"total":1,"successful":1,"failed":0},"_seq_no":0,"_primary_term":1}""");
        IndexResponse    response = await client.IndexAsync(Samples.Order(), static i => i.Id(7));

        Assert.Multiple(() =>
                        {
                            Assert.That(response.IsValid, Is.True, response.DebugInformation);
                            Assert.That(Encoding.UTF8.GetString(response.ApiCall.RequestBodyInBytes!), Does.Contain("\"customer\" : \"Zoë\""));
                        });
    }

    [Test] public async Task Get_ReadsTheSourceWithJakarJson()
    {
        OpenSearchClient   client   = Client($$"""{"_index":"orders","_id":"7","_version":1,"_seq_no":0,"_primary_term":1,"found":true,"_source":{{Samples.Order().ToJson()}}}""");
        GetResponse<Order> response = await client.GetAsync<Order>(7);

        Assert.Multiple(() =>
                        {
                            Assert.That(response.Found,  Is.True, response.DebugInformation);
                            Assert.That(response.Source, Is.EqualTo(Samples.Order()));
                        });
    }

    [Test] public async Task OtherTypes_UseTheBuiltInSerializer()
    {
        OpenSearchClient client   = Client("""{"_index":"orders","_id":"1","_version":1,"result":"created","_shards":{"total":1,"successful":1,"failed":0},"_seq_no":0,"_primary_term":1}""");
        IndexResponse    response = await client.IndexAsync(new PlainDto("x"), static i => i.Id(1));

        Assert.That(Encoding.UTF8.GetString(response.ApiCall.RequestBodyInBytes!), Does.Not.Contain(Samples.JAKAR_SEPARATOR));
    }
}
