# Jakar.Json.Rebus

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) for [Rebus](https://github.com/rebus-org/Rebus).

```csharp
Configure.With(activator)
         .Transport(t => t.UseRabbitMq(connectionString, "orders"))
         .Serialization(s => s.UseJakarJson())
         .Start();
```

- **Models:** message bodies of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written and read by Jakar.Json.
- **Everything else** stays with Rebus' System.Text.Json serializer: its headers (`rbs2-msg-type`, `rbs2-content-type`) and every other type. Messages stay readable by other Rebus endpoints.
- Pass `UseJakarJson(options)` to configure the System.Text.Json side. Jakar.Json's converter is added to those options.

Uses [Jakar.Json.SystemTextJson](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.SystemTextJson/README.md)'s converter.
