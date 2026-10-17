# Jakar.Json.AzureCore

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) as an Azure SDK `ObjectSerializer` (Azure.Core). One serializer covers every SDK that accepts one:

| SDK | Where it goes |
|---|---|
| Azure AI Search | `new SearchClientOptions { Serializer = serializer }` |
| Azure Functions (isolated worker) | `services.Configure<WorkerOptions>(o => o.Serializer = serializer)` |
| Event Grid, Service Bus, Event Hubs, Storage Queues payloads | `BinaryData.FromObjectAsJson` → `serializer.Serialize(value)`; `data.ToObject<T>(serializer)` |
| Anything else taking `ObjectSerializer` | its options |

```csharp
ObjectSerializer serializer = new JakarJsonObjectSerializer(new JsonObjectSerializer(JsonSerializerOptions.Web));
```

- **Models:** `[JsonModel]` types, and arrays, lists and collection interfaces of them, are read and written by Jakar.Json.
- **Other types** go to the fallback, which also answers `IMemberNameConverter`. Azure Search builds `$select`, `$filter` and `$orderby` field names from it, so give the fallback the naming your models use.
- **Native AOT:** the parameterless constructor uses `JsonObjectSerializer.Default`, which is reflection-based, and is annotated as such. For a trimmed app, pass a `JsonObjectSerializer` built on a `JsonSerializerContext`.
- **Errors:** malformed payloads throw `JsonReadException`, which derives from `JsonException`.
