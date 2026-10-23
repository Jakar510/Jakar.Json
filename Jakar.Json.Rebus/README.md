# Jakar.Json.Rebus

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) for [Rebus](https://github.com/rebus-org/Rebus).

```csharp
Configure.With(activator)
         .Transport(t => t.UseRabbitMq(connectionString, "orders"))
         .Serialization(s => s.UseJakarJson())
         .Start();
```

- **Models:** message bodies of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written and read by Jakar.Json, straight to and from the transport message's bytes (`JakarJsonRebusSerializer`).
- **Headers:** the type (`rbs2-msg-type`, from Rebus' type name convention) and content type (`application/json;charset=utf-8`) headers are the ones Rebus' JSON serializer writes, so endpoints without Jakar.Json still read the messages.
- **Everything else** goes to the serializer configured before `UseJakarJson()`, or Rebus' default. To configure it, set it up first: `s.UseSystemTextJson(options); s.UseJakarJson();`.
