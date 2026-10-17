# Jakar.Json.SystemTextJson

Runs [Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) inside System.Text.Json. `[JsonModel]` types, and arrays, lists and collection interfaces of them, are read and written by Jakar.Json. Every other type keeps System.Text.Json's handling.

```csharp
JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web).AddJakarJson();
```

This reaches every framework you configure through `JsonSerializerOptions`, with no package of its own:

| Host | Where the options go |
|---|---|
| ASP.NET Core minimal APIs / MVC (System.Text.Json) | `ConfigureHttpJsonOptions(o => o.SerializerOptions.AddJakarJson())`, `AddJsonOptions(o => o.JsonSerializerOptions.AddJakarJson())` |
| SignalR JSON protocol | `AddJsonProtocol(o => o.PayloadSerializerOptions.AddJakarJson())` |
| HttpClient (`System.Net.Http.Json`) | `GetFromJsonAsync<T>(url, options)`, `PostAsJsonAsync(url, value, options)` |
| Microsoft.Extensions.AI | `AIFunctionFactory.Create(..., serializerOptions: options)`, `AIJsonUtilities` |
| Npgsql `json`/`jsonb` | `dataSourceBuilder.ConfigureJsonOptions(options)` |
| Dapr | `new DaprClientBuilder().UseJsonSerializationOptions(options)` |
| Refit, RestSharp, Flurl (System.Text.Json mode) | their System.Text.Json serializer's options |
| Google Cloud Functions, gRPC JSON transcoding, HotChocolate | their `JsonSerializerOptions` |

## How it works

`JakarJsonConverterFactory` hands System.Text.Json a `JakarJsonConverter<T>` for each model type:

- **Writing:** Jakar.Json writes the value into a pooled buffer, which is copied into the `Utf8JsonWriter` as is. The model keeps its own `[JsonModel]` naming, spacing and indentation.
- **Reading:** System.Text.Json buffers the whole value before it calls a converter. The converter takes the value's raw UTF-8 and Jakar.Json parses it.

The bridge costs one copy each way, plus System.Text.Json scanning the value to find where it ends. The model's own reading and writing still run on Jakar.Json. When the host lets you hand it bytes directly, a dedicated integration avoids the bridge: ASP.NET Core, HttpClient, caching, messaging and so on.

## Native AOT

System.Text.Json needs one converter instance per type. Normally the factory creates them on demand. Under Native AOT, list the types it converts instead:

```csharp
options.AddJakarJson(f => f.Add<Order>().Add<List<Order>>());
```

You can also add `new JakarJsonConverter<Order>()` to `options.Converters` yourself. This works alongside a source-generated `JsonSerializerContext`.

## Errors

Malformed JSON throws `JsonReadException`, which derives from System.Text.Json's `JsonException`, so hosts report it the way they report their own errors. JSON `null` reads as `null` for reference types, and throws `JsonException` for a struct model.
