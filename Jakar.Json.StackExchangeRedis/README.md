# Jakar.Json.StackExchangeRedis

Stores `[JsonModel]` values, and arrays, lists and collection interfaces of them, in Redis as [Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) UTF-8, using [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis).

```csharp
IDatabase db = multiplexer.GetDatabase();

await db.StringSetJakarJsonAsync("order:7", order, expiry: TimeSpan.FromMinutes(5));
Order? order = await db.StringGetJakarJsonAsync<Order>("order:7");

await db.HashSetJakarJsonAsync("customer:7", "latest", order);
Order? latest = await db.HashGetJakarJsonAsync<Order>("customer:7", "latest");
```

There are sync and async versions of each.

- **Writes** send the pooled buffer Jakar.Json wrote into, so there's no intermediate `byte[]`. The exception is `CommandFlags.FireAndForget`: that command returns before it's sent, so the value is copied first.
- **Reads** use `StringGetLease` / `HashGetLease`, which parse from a pooled lease without copying.
- **Missing** keys or fields return `default`.
- **Errors:** malformed values throw `JsonReadException`, and types Jakar.Json can't serialize throw `NotSupportedException`.

For an `IDistributedCache` backed by Redis, see [Jakar.Json.Caching](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.Caching/README.md).
