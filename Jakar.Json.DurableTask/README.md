# Jakar.Json.DurableTask

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) as the [Durable Task](https://github.com/microsoft/durabletask-dotnet) data converter, for Durable Functions (isolated) and the Durable Task Scheduler.

```csharp
services.AddDurableTaskClient(b => b.Configure(o => o.DataConverter = new JakarJsonDataConverter()));
services.AddDurableTaskWorker(b => b.Configure(o => o.DataConverter = new JakarJsonDataConverter()));
```

- **Models:** orchestration and activity inputs, outputs, custom statuses and external events of `[JsonModel]` types, and of arrays, lists and collection interfaces of them, are written and read by Jakar.Json.
- **Other types** go to the fallback, `JsonDataConverter.Default` by default.
- Durable Task stores payloads as strings, so Jakar.Json's UTF-8 is transcoded once each way.
