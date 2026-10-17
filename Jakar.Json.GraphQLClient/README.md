# Jakar.Json.GraphQLClient

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) for [GraphQL.Client](https://github.com/graphql-dotnet/graphql-client).

```csharp
using GraphQLHttpClient client = new("https://api.example.com/graphql", JakarJsonGraphQL.CreateSerializer());

GraphQLResponse<OrderQuery> response = await client.SendQueryAsync<OrderQuery>(request);
```

- **Models:** `[JsonModel]` types, and arrays, lists and collection interfaces of them, are read by Jakar.Json in response data and written by it in variables. Subscriptions over WebSockets use the same serializer.
- **Everything else** keeps System.Text.Json: the GraphQL envelope (`data`, `errors`, `extensions`) and every other type.
- `CreateSerializer(o => ...)` configures the System.Text.Json side.

Uses [Jakar.Json.SystemTextJson](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.SystemTextJson/README.md)'s converter.
