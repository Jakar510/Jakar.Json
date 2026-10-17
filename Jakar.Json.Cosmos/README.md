# Jakar.Json.Cosmos

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) as the Azure Cosmos DB serializer.

```csharp
CosmosClient client = new(connectionString, new CosmosClientOptions { Serializer = new JakarJsonCosmosSerializer() });
```

- **Items** of `[JsonModel]` types, and arrays, lists and collection interfaces of them, are read and written by Jakar.Json. Responses are parsed straight from the SDK's buffer when it's a `MemoryStream`.
- **Other types** go to the fallback, a System.Text.Json `CosmosLinqSerializer` with camelCase by default. Pass your own as `new JakarJsonCosmosSerializer(fallback)`.
- **LINQ:** queries are translated using the fallback's member names (`SerializeMemberName`). Give it the naming your models use, so `o => o.Customer == "x"` queries `root["customer"]`, the property Jakar.Json writes. The default camelCase fallback matches `[JsonModel(Naming = JsonNaming.CamelCase)]`.
- **Streams:** `FromStream<Stream>` returns the stream as is, and other reads dispose the stream, as the SDK expects.

The Cosmos SDK isn't trimming-safe, so this package isn't marked AOT-compatible.
