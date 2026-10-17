# Jakar.Json.EasyNetQ

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) as [EasyNetQ](https://easynetq.com)'s (RabbitMQ) message serializer.

```csharp
services.AddEasyNetQ("host=localhost").UseJakarJson();
```

- **Publishing:** messages of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written into a pooled buffer that EasyNetQ takes ownership of and returns after publishing. No intermediate `byte[]`.
- **Consuming:** deliveries are parsed straight from the received memory.
- **Other types** go to the fallback, EasyNetQ's `SystemTextJsonSerializer` by default. For trimmed apps, pass one built on a `JsonSerializerContext`: `UseJakarJson(fallback)`.
