// Jakar.Json.GraphQLClient
// 10/17/2026

using System.Text.Json;
using GraphQL.Client.Serializer.SystemTextJson;
using Jakar.Json.SystemTextJson;



namespace Jakar.Json.GraphQLClient;


public static class JakarJsonGraphQL
{
    /// <summary>
    ///     GraphQL.Client's System.Text.Json serializer, with <c>[JsonModel]</c> types (and arrays, lists and collection interfaces of them) in responses and variables read and written by
    ///     Jakar.Json: <c>new GraphQLHttpClient(endpoint, JakarJsonGraphQL.CreateSerializer())</c>. The GraphQL envelope (<c>data</c>, <c>errors</c>, extensions) keeps System.Text.Json.
    /// </summary>
    /// <param name="configure"> Further System.Text.Json options (applied before Jakar.Json's converter is added). </param>
    public static SystemTextJsonSerializer CreateSerializer( Action<JsonSerializerOptions>? configure = null ) => new(options =>
                                                                                                                    {
                                                                                                                        configure?.Invoke(options);
                                                                                                                        options.AddJakarJson();
                                                                                                                    });
}
