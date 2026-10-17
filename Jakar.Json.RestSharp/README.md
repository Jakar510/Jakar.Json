# Jakar.Json.RestSharp

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) as [RestSharp](https://restsharp.dev)'s serializer.

```csharp
RestClient client = new(new RestClientOptions("https://api.example.com"), configureSerialization: s => s.UseJakarJson());

Order? order = await client.GetAsync<Order>("orders/7");
Order? saved = await client.PostAsync<Order>(new RestRequest("orders").AddJsonBody(order));
```

- **Models:** request and response bodies of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written and read by Jakar.Json. Responses are parsed from their raw bytes, not from a decoded string.
- **Other types** go to the fallback, RestSharp's `SystemTextJsonSerializer` by default. The fallback also decides which response content types count as JSON.
- RestSharp's serializer interface writes request bodies as strings, so a request body is transcoded once from Jakar.Json's UTF-8.
