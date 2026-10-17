// Jakar.Json.GraphQLClient.Tests
// 10/17/2026

using System.Net;
using System.Text;
using GraphQL;
using GraphQL.Client.Http;



namespace Jakar.Json.GraphQLClient.Tests;


public sealed class OrderQuery
{
    public Order?       Order  { get; set; }
    public List<Order>? Orders { get; set; }
}



/// <summary> A GraphQL client over HTTP against a stub server. </summary>
[TestFixture]
public sealed class GraphQLClientTests
{
    [Test] public async Task Response_ModelsAreReadByJakarJson()
    {
        string json = "{\"data\":{\"order\":" + Samples.Order().ToJson() + ",\"orders\":" + JsonCodec.ToJsonArray(Samples.Orders()) + "}}";

        using GraphQLHttpClient client = Client(_ => json);
        GraphQLResponse<OrderQuery> response = await client.SendQueryAsync<OrderQuery>(new GraphQLRequest("{ order { id } orders { id } }"));

        Assert.Multiple(() =>
                        {
                            Assert.That(response.Errors,       Is.Null.Or.Empty);
                            Assert.That(response.Data.Order,   Is.EqualTo(Samples.Order()));
                            Assert.That(response.Data.Orders,  Is.EqualTo(Samples.Orders()));
                        });
    }

    [Test] public async Task Variables_ModelsAreWrittenByJakarJson()
    {
        string? request = null;

        using GraphQLHttpClient client = Client(body =>
                                                {
                                                    request = body;
                                                    return """{"data":{"order":null}}""";
                                                });

        await client.SendMutationAsync<OrderQuery>(new GraphQLRequest("mutation($order: OrderInput!) { place(order: $order) { id } }", new { order = Samples.Order() }));

        Assert.That(request, Does.Contain(Samples.JAKAR_SEPARATOR).And.Contain("\"query\""), "the variables' model is Jakar.Json's, the envelope System.Text.Json's");
    }

    [Test] public async Task Errors_StillParse()
    {
        using GraphQLHttpClient     client   = Client(_ => """{"data":null,"errors":[{"message":"boom"}]}""");
        GraphQLResponse<OrderQuery> response = await client.SendQueryAsync<OrderQuery>(new GraphQLRequest("{ order { id } }"));

        Assert.That(response.Errors!.Single().Message, Is.EqualTo("boom"));
    }


    private static GraphQLHttpClient Client( Func<string, string> respond ) => new(new GraphQLHttpClientOptions { EndPoint = new Uri("https://graphql.test/graphql") }, JakarJsonGraphQL.CreateSerializer(), new HttpClient(new Stub(respond)));



    private sealed class Stub( Func<string, string> respond ) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken ) => new(HttpStatusCode.OK) { Content = new StringContent(respond(await request.Content!.ReadAsStringAsync(cancellationToken)), Encoding.UTF8, "application/json") };
    }
}
