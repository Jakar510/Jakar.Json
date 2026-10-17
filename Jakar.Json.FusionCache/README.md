# Jakar.Json.FusionCache

Makes [Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) the serializer of [FusionCache](https://github.com/ZiggyCreatures/FusionCache)'s distributed cache (Redis, SQL Server, any `IDistributedCache`).

```csharp
builder.Services.AddFusionCache()
                .WithDistributedCache(redis)
                .WithJakarJsonSerializer();
```

Or on a cache you build yourself: `cache.SetupDistributedCache(distributedCache, new FusionCacheJakarJsonSerializer())`.

## What it writes

FusionCache doesn't store your value alone: it stores a `FusionCacheDistributedEntry<TValue>` holding the value, its timestamps, tags and metadata (stale flag, ETag, size, priority, eager expiration). This serializer writes:

- **The entry** as a compact binary envelope. It's a few dozen bytes plus the tags, instead of a JSON object with long member names.
- **The value** in one of two ways:
  - **Jakar.Json UTF-8 JSON** for `[JsonModel]` types, including structs, and for arrays, `List<T>` and the collection interfaces of them. That covers anything `JsonCodecRegistry` resolves.
  - **Binary** for common scalars:
    - text and bytes: `string`, `char`, `byte[]` (stored as is, not base64);
    - `bool`;
    - every integer type, `float`, `double` and `decimal`;
    - `Guid`;
    - dates and times: `DateTime` (its `Kind` is kept), `DateTimeOffset` (its offset is kept), `DateOnly`, `TimeOnly`, `TimeSpan`;
    - enums, as their underlying integer.

Writing allocates only the returned `byte[]`: each thread reuses one scratch buffer, released if a value grew it past 1 MB. Reading allocates the value and the entry. The value's type is resolved once per type, so there are no lookups per call.

## Other types: the fallback

Types Jakar.Json can't serialize can go to another serializer. Examples are classes without `[JsonModel]` and `Nullable<T>` scalars such as `int?`:

```csharp
.WithJakarJsonSerializer(fallback: new FusionCacheSystemTextJsonSerializer())
```

- Without a fallback, caching such a type throws `NotSupportedException`. `FusionCacheJakarJsonSerializer.IsSupported<T>()` tells you ahead of time.
- Every payload this serializer writes starts with `0xFF 'J'`. JSON and UTF-8 text can't start with that byte, so reads that don't carry the header go to the fallback.
- **Switching an existing cache:** set its previous serializer as the fallback. Entries already in the distributed cache stay readable, and new ones are written by Jakar.Json.

## Native AOT

Entries are recognized without reflection. Under Native AOT, though, the codec for each `FusionCacheDistributedEntry<TValue>` can't be created on demand, so register each value type the cache stores at startup:

```csharp
JakarJsonFusionCache.Register<Order>();
JakarJsonFusionCache.Register<List<Order>>();
```

Elsewhere this isn't needed: the codec is created on first use.

## Errors

Corrupt or truncated payloads, malformed JSON and unknown envelope versions throw `FusionCacheSerializationException`. FusionCache then rethrows or logs the error, depending on `ReThrowSerializationExceptions`.

## Versioning

Versions follow `<.NET major>.<library major>.<library minor>`: 10.1.0 targets .NET 10. The envelope carries its own format version, so a future version can still read today's entries. See the [repository README](https://github.com/Jakar510/Jakar.Json#versioning).
