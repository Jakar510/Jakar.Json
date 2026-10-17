# Jakar.Json.OpenSearch

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) as the source serializer of the [OpenSearch .NET client](https://github.com/opensearch-project/opensearch-net) (`OpenSearch.Client`).

```csharp
OpenSearchClient client = new(new ConnectionSettings(new SingleNodeConnectionPool(uri), JakarJsonOpenSearchSerializer.Factory));
```

- **Models:** documents (`_source`) of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written and read by Jakar.Json.
- **Other types** go to the client's built-in serializer.
- **What stays with the client:** request and response envelopes always use the client's own serializer.

The OpenSearch client isn't trimming-safe, so this package isn't marked AOT-compatible.
