# Jakar.Json.Cap

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) for [CAP](https://cap.dotnetcore.xyz) (`DotNetCore.CAP`), the outbox and event bus.

```csharp
services.AddCap(x =>
{
    x.UseJakarJson();
    x.UsePostgreSql(connectionString);
    x.UseRabbitMQ("localhost");
});
```

- **Models:** message payloads of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written and read by Jakar.Json. That covers the published messages, the stored outbox and inbox rows, and the consumer parameters.
- **Everything else** keeps System.Text.Json: CAP's headers and every other type.

Uses [Jakar.Json.SystemTextJson](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.SystemTextJson/README.md)'s converter.
