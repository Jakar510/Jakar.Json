# Jakar.Json.Elasticsearch

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) as the source serializer of the [Elasticsearch .NET client](https://github.com/elastic/elasticsearch-net) (`Elastic.Clients.Elasticsearch` 8+).

```csharp
ElasticsearchClient client = new(new ElasticsearchClientSettings(new SingleNodePool(uri), JakarJsonSourceSerializer.Factory()));
```

- **Models:** documents (`_source`) of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written and read by Jakar.Json. That covers index, bulk, get, search hits and so on.
- **Other types** go to the client's `DefaultSourceSerializer`, which you can configure with `Factory(o => ...)`.
- **What stays with the client:** request and response envelopes (queries, aggregations, metadata) always use the client's own serializer.
- **Field names** in queries come from the client's property mapping. Keep it consistent with your models' naming: camelCase by default, matching `[JsonModel(Naming = JsonNaming.CamelCase)]`.

The Elasticsearch client isn't trimming-safe, so this package isn't marked AOT-compatible.
