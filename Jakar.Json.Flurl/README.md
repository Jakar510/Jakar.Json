# Jakar.Json.Flurl

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) as [Flurl.Http](https://flurl.dev)'s JSON serializer.

```csharp
FlurlHttp.Clients.WithDefaults(b => b.Settings.UseJakarJson());
// or per client
IFlurlClient client = new FlurlClient("https://api.example.com");
client.Settings.UseJakarJson();

Order order = await client.Request("orders", 7).GetJsonAsync<Order>();
```

- **Models:** request and response bodies of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written and read by Jakar.Json. Responses are read from the stream.
- **Other types** go to the fallback, the serializer the settings already had (Flurl's `DefaultJsonSerializer`).
- Flurl's serializer interface writes request bodies as strings, so a request body is transcoded once from Jakar.Json's UTF-8.
