# Jakar.Json.Quartz

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) for [Quartz.NET](https://www.quartz-scheduler.net) job data.

Quartz's persistent job stores only round-trip simple values (strings, numbers, dates, enums, ...). They expect anything structured to be stored as a string, and reject it otherwise. These helpers store `[JsonModel]` values as Jakar.Json text, so they work with any job store and any Quartz serializer.

```csharp
IJobDetail job = JobBuilder.Create<ShipOrderJob>()
    .WithIdentity("ship-7")
    .UsingJakarJson("order", order)
    .Build();

public sealed class ShipOrderJob : IJob
{
    public ValueTask Execute(IJobExecutionContext context, CancellationToken token)
    {
        Order order = context.MergedJobDataMap.GetJakarJson<Order>("order")!;
        // ...
    }
}
```

- `map.PutJakarJson(key, value)` stores the value.
- `map.GetJakarJson<T>(key)` reads it back. It returns `default` when the key is missing, and throws `InvalidCastException` when the entry isn't text.
