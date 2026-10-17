# Jakar.Json.Caching

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) for Microsoft's caches.

## HybridCache

```csharp
builder.Services.AddHybridCache().AddJakarJsonSerializer();
```

HybridCache asks for a serializer type by type. The factory provides one for every `[JsonModel]` type, and for arrays, lists and collection interfaces of them. It declines every other type, which HybridCache's default serializer (System.Text.Json) keeps, so you can adopt Jakar.Json one model at a time.

The serializer writes straight into HybridCache's buffer and reads its multi-segment payloads directly: no intermediate `byte[]` or string either way.

## IDistributedCache

```csharp
await cache.SetJakarJsonAsync("order:7", order, new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(5) });
Order? order = await cache.GetJakarJsonAsync<Order>("order:7");
```

- A missing key returns `default`.
- Values are stored as Jakar.Json UTF-8.
- When the cache implements `IBufferDistributedCache` (Redis, SQL Server and others in .NET 9+), values go through pooled buffers. Otherwise the helpers fall back to the `byte[]` API.
- Types Jakar.Json can't serialize throw `NotSupportedException`, and corrupt values throw `JsonReadException`.

For FusionCache, see [Jakar.Json.FusionCache](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.FusionCache/README.md).
