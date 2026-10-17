// Jakar.Json.FusionCache.Tests
// 10/17/2026

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using ZiggyCreatures.Caching.Fusion;
using ZiggyCreatures.Caching.Fusion.Internals;
using ZiggyCreatures.Caching.Fusion.Internals.Distributed;
using ZiggyCreatures.Caching.Fusion.Serialization;



namespace Jakar.Json.FusionCache.Tests;


[TestFixture]
public sealed class SerializerTests
{
    private static readonly CachedOrder __order = new()
                                                  {
                                                      Id       = 7,
                                                      Customer = "Zoë",
                                                      Total    = 12.50m,
                                                      Lines    = [new CachedLine { Sku = "A-1", Quantity = 2, Price = 6.25m }]
                                                  };

    private readonly FusionCacheJakarJsonSerializer __serializer = new();


    // ─── Entries ─────────────────────────────────────────────────────────────

    [Test] public void Entry_Model_RoundTrips()
    {
        FusionCacheDistributedEntry<CachedOrder> entry = Entry(__order, ["orders", "customer:7"], new FusionCacheEntryMetadata(true, 111, "\"etag\"", 222, 333, 4));
        FusionCacheDistributedEntry<CachedOrder> back  = RoundTrip(entry);

        Assert.Multiple(() =>
                        {
                            Assert.That(back.Value,                      Is.EqualTo(__order));
                            Assert.That(back.Timestamp,                  Is.EqualTo(entry.Timestamp));
                            Assert.That(back.LogicalExpirationTimestamp, Is.EqualTo(entry.LogicalExpirationTimestamp));
                            Assert.That(back.Tags,                       Is.EqualTo(entry.Tags));
                            Assert.That(back.Metadata!.IsStale,                  Is.True);
                            Assert.That(back.Metadata.EagerExpirationTimestamp, Is.EqualTo(111));
                            Assert.That(back.Metadata.ETag,                     Is.EqualTo("\"etag\""));
                            Assert.That(back.Metadata.LastModifiedTimestamp,    Is.EqualTo(222));
                            Assert.That(back.Metadata.Size,                     Is.EqualTo(333));
                            Assert.That(back.Metadata.Priority,                 Is.EqualTo((byte)4));
                        });
    }

    [Test] public void Entry_WithoutMetadataOrTags_RoundTrips()
    {
        FusionCacheDistributedEntry<CachedOrder> back = RoundTrip(Entry(__order, null, null));

        Assert.Multiple(() =>
                        {
                            Assert.That(back.Value,    Is.EqualTo(__order));
                            Assert.That(back.Tags,     Is.Null);
                            Assert.That(back.Metadata, Is.Null);
                        });
    }

    [Test] public void Entry_EmptyTagsAndPartialMetadata_RoundTrip()
    {
        FusionCacheDistributedEntry<CachedOrder> back = RoundTrip(Entry(__order, [], new FusionCacheEntryMetadata(false, null, null!, null, 42, null)));

        Assert.Multiple(() =>
                        {
                            Assert.That(back.Tags,                              Is.Empty);
                            Assert.That(back.Metadata!.IsStale,                  Is.False);
                            Assert.That(back.Metadata.EagerExpirationTimestamp, Is.Null);
                            Assert.That(back.Metadata.ETag,                     Is.Null);
                            Assert.That(back.Metadata.Size,                     Is.EqualTo(42));
                            Assert.That(back.Metadata.Priority,                 Is.Null);
                        });
    }

    [Test] public void Entry_NullValue_RoundTrips() => Assert.That(RoundTrip(Entry<CachedOrder?>(null, null, null)).Value, Is.Null);

    [Test] public void Entry_ModelCollectionsAndStructs_RoundTrip()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(RoundTrip(Entry(new List<CachedOrder> { __order, __order with { Id = 8 } }, null, null)).Value, Is.EqualTo(new List<CachedOrder> { __order, __order with { Id = 8 } }));
                            Assert.That(RoundTrip(Entry(new[] { __order }, null, null)).Value,                                         Is.EqualTo(new[] { __order }));
                            Assert.That(RoundTrip(Entry(new CachedPoint { X = -3, Y = 9 }, null, null)).Value,                          Is.EqualTo(new CachedPoint { X = -3, Y = 9 }));
                        });
    }

    [Test] public void Entry_Scalars_RoundTrip()
    {
        Assert.Multiple(() =>
                        {
                            AssertScalar("text ✓ 𝄞");
                            AssertScalar("");
                            AssertScalar(new byte[] { 0, 1, 255 });
                            AssertScalar(true);
                            AssertScalar((byte)200);
                            AssertScalar((sbyte)-100);
                            AssertScalar((short)-12345);
                            AssertScalar((ushort)54321);
                            AssertScalar(int.MinValue);
                            AssertScalar(uint.MaxValue);
                            AssertScalar(long.MinValue);
                            AssertScalar(ulong.MaxValue);
                            AssertScalar(1.5f);
                            AssertScalar(double.NaN);
                            AssertScalar(-0.1m);
                            AssertScalar(79228162514264337593543950335m);
                            AssertScalar('é');
                            AssertScalar(Guid.NewGuid());
                            AssertScalar(new DateTime(2026, 10, 17, 1, 2, 3, DateTimeKind.Utc).AddTicks(7));
                            AssertScalar(new DateTime(2026, 10, 17, 1, 2, 3, DateTimeKind.Unspecified));
                            AssertScalar(new DateTimeOffset(2026, 10, 17, 1, 2, 3, TimeSpan.FromHours(-5.5)).AddTicks(9));
                            AssertScalar(new DateOnly(2026, 10, 17));
                            AssertScalar(new TimeOnly(23, 59, 58, 123));
                            AssertScalar(TimeSpan.FromTicks(-123456789));
                            AssertScalar(ByteColor.Blue);
                            AssertScalar(LongFlags.High);
                            AssertScalar(DayOfWeek.Friday);
                        });
    }

    [Test] public void Entry_KeepsDateTimeKind() => Assert.That(RoundTrip(Entry(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local), null, null)).Value.Kind, Is.EqualTo(DateTimeKind.Local));


    // ─── Bare values (not inside an entry) ───────────────────────────────────

    [Test] public void BareValue_RoundTrips()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(__serializer.Deserialize<CachedOrder>(__serializer.Serialize(__order)), Is.EqualTo(__order));
                            Assert.That(__serializer.Deserialize<int>(__serializer.Serialize(42)),               Is.EqualTo(42));
                            Assert.That(__serializer.Deserialize<string>(__serializer.Serialize<string>(null)),   Is.Null);
                        });
    }

    [Test] public void BareValueAndEntry_AreNotMistakenForEachOther()
    {
        byte[] bare  = __serializer.Serialize(__order);
        byte[] entry = __serializer.Serialize(Entry(__order, null, null));

        Assert.Multiple(() =>
                        {
                            Assert.Throws<FusionCacheSerializationException>(() => __serializer.Deserialize<FusionCacheDistributedEntry<CachedOrder>>(bare));
                            Assert.Throws<FusionCacheSerializationException>(() => __serializer.Deserialize<CachedOrder>(entry));
                        });
    }


    // ─── Format ──────────────────────────────────────────────────────────────

    [Test] public void Payload_HasHeaderAndJakarJsonValue()
    {
        byte[] payload = __serializer.Serialize(Entry(__order, null, null));

        Assert.Multiple(() =>
                        {
                            Assert.That(payload.AsSpan(0, 4).ToArray(),        Is.EqualTo(new byte[] { 0xFF, (byte)'J', 1, 1 }));
                            Assert.That(Encoding.UTF8.GetString(payload),      Does.Contain("\"Customer\" : \"Zoë\""), "the value is Jakar.Json's UTF-8 JSON");
                            Assert.That(payload[^1],                           Is.EqualTo((byte)'}'),                  "the value runs to the end");
                        });
    }

    [Test] public void Serialize_ReusesItsBufferButReturnsIndependentArrays()
    {
        byte[] first  = __serializer.Serialize(Entry(__order, null, null));
        byte[] copy   = first.ToArray();
        byte[] second = __serializer.Serialize(Entry(__order with { Customer = "Someone else entirely" }, null, null));

        Assert.Multiple(() =>
                        {
                            Assert.That(first,  Is.EqualTo(copy));
                            Assert.That(second, Is.Not.EqualTo(first));
                        });
    }


    // ─── Unsupported types and the fallback ──────────────────────────────────

    [Test] public void Unsupported_WithoutFallback_Throws()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(FusionCacheJakarJsonSerializer.IsSupported<FusionCacheDistributedEntry<PlainDto>>(), Is.False);
                            Assert.That(FusionCacheJakarJsonSerializer.IsSupported<int?>(),                                  Is.False, "Nullable<T> scalars need the fallback");
                            Assert.That(FusionCacheJakarJsonSerializer.IsSupported<FusionCacheDistributedEntry<CachedOrder>>(), Is.True);
                            Assert.Throws<NotSupportedException>(() => __serializer.Serialize(Entry(new PlainDto("x"), null, null)));
                        });
    }

    [Test] public void Unsupported_GoesToFallback_AndReadsBackThroughIt()
    {
        RecordingFallback              fallback   = new();
        FusionCacheJakarJsonSerializer serializer = new(fallback);
        PlainDto                       dto        = new("x");

        byte[] payload = serializer.Serialize(dto);

        Assert.Multiple(() =>
                        {
                            Assert.That(fallback.Serialized,                            Is.EqualTo(1));
                            Assert.That(serializer.Deserialize<PlainDto>(payload),       Is.EqualTo(dto));
                            Assert.That(fallback.Deserialized,                          Is.EqualTo(1));
                            Assert.That(serializer.Serialize(__order)[0],               Is.EqualTo((byte)0xFF), "supported types never reach the fallback");
                            Assert.That(fallback.Serialized,                            Is.EqualTo(1));
                        });
    }

    [Test] public void FallbackPayloadForSupportedType_IsReadByFallback()
    {
        // e.g. entries written before switching to this serializer
        RecordingFallback              fallback   = new();
        FusionCacheJakarJsonSerializer serializer = new(fallback);

        Assert.That(serializer.Deserialize<CachedOrder>(fallback.Serialize(__order)), Is.EqualTo(__order));
    }

    [Test] public void ForeignPayload_WithoutFallback_Throws() => Assert.Throws<FusionCacheSerializationException>(() => __serializer.Deserialize<CachedOrder>("{\"Id\":1}"u8.ToArray()));


    // ─── Corrupt payloads ────────────────────────────────────────────────────

    [Test] public void Corrupt_Throws()
    {
        byte[] payload = __serializer.Serialize(Entry(__order, ["a"], new FusionCacheEntryMetadata(false, 1, "e", 2, 3, 4)));

        byte[] wrongVersion = payload.ToArray();
        wrongVersion[2] = 99;

        byte[] badJson = payload.ToArray();
        badJson[^1] = (byte)'!';

        Assert.Multiple(() =>
                        {
                            for ( int length = 4; length < payload.Length - 1; length++ )
                            {
                                byte[] truncated = payload[..length];
                                Assert.Throws<FusionCacheSerializationException>(() => __serializer.Deserialize<FusionCacheDistributedEntry<CachedOrder>>(truncated), $"truncated to {length} bytes");
                            }

                            Assert.Throws<FusionCacheSerializationException>(() => __serializer.Deserialize<FusionCacheDistributedEntry<CachedOrder>>(wrongVersion));
                            Assert.Throws<FusionCacheSerializationException>(() => __serializer.Deserialize<FusionCacheDistributedEntry<CachedOrder>>(badJson));
                            Assert.Throws<FusionCacheSerializationException>(() => __serializer.Deserialize<int>([0xFF, (byte)'J', 1, 0, 1, 1, 2]), "wrong scalar size");
                        });
    }


    // ─── Registration and async ──────────────────────────────────────────────

    [Test] public void Register_AcceptsSupportedAndRejectsUnsupported()
    {
        Assert.Multiple(() =>
                        {
                            Assert.DoesNotThrow(JakarJsonFusionCache.Register<CachedLine>);
                            Assert.DoesNotThrow(JakarJsonFusionCache.Register<CachedLine>); // idempotent
                            Assert.Throws<NotSupportedException>(JakarJsonFusionCache.Register<PlainDto>);
                        });
    }

    [Test] public async Task Async_RoundTrips()
    {
        byte[] payload = await __serializer.SerializeAsync(Entry(__order, ["x"], null));
        Assert.That((await __serializer.DeserializeAsync<FusionCacheDistributedEntry<CachedOrder>>(payload))!.Value, Is.EqualTo(__order));
    }


    // ─── End to end ──────────────────────────────────────────────────────────

    [Test] public async Task FusionCache_SharesValuesThroughTheDistributedCache()
    {
        RecordingDistributedCache distributed = new();

        using ZiggyCreatures.Caching.Fusion.FusionCache writer = Cache(distributed);
        using ZiggyCreatures.Caching.Fusion.FusionCache reader = Cache(distributed); // its own memory cache, so reads come from the distributed cache

        await writer.SetAsync("order:7", __order, tags: ["orders"]);
        await writer.SetAsync("count",   42);

        await Assert.MultipleAsync(async () =>
                        {
                            Assert.That(await reader.GetOrDefaultAsync<CachedOrder>("order:7"), Is.EqualTo(__order));
                            Assert.That(await reader.GetOrDefaultAsync<int>("count"),           Is.EqualTo(42));
                            Assert.That(distributed.Written,                                    Is.Not.Empty.And.All.Matches<byte[]>(static x => x is [0xFF, (byte)'J', ..]), "stored by this serializer");
                        });
    }


    private static ZiggyCreatures.Caching.Fusion.FusionCache Cache( IDistributedCache distributed )
    {
        ZiggyCreatures.Caching.Fusion.FusionCache cache = new(new FusionCacheOptions { DefaultEntryOptions = new FusionCacheEntryOptions { Duration = TimeSpan.FromMinutes(5) } });
        cache.SetupDistributedCache(distributed, new FusionCacheJakarJsonSerializer());
        return cache;
    }

    private static FusionCacheDistributedEntry<T> Entry<T>( T value, string[]? tags, FusionCacheEntryMetadata? metadata ) => new(value, 638_000_000_000_000_000, 638_000_000_100_000_000, tags!, metadata!);

    private FusionCacheDistributedEntry<T> RoundTrip<T>( FusionCacheDistributedEntry<T> entry ) => __serializer.Deserialize<FusionCacheDistributedEntry<T>>(__serializer.Serialize(entry))!;

    private void AssertScalar<T>( T value ) => Assert.That(RoundTrip(Entry(value, null, null)).Value, Is.EqualTo(value), typeof(T).Name);



    /// <summary> An in-memory distributed cache that keeps what was written to it. </summary>
    private sealed class RecordingDistributedCache : IDistributedCache
    {
        private readonly MemoryDistributedCache __inner = new(Options.Create(new MemoryDistributedCacheOptions()));

        public List<byte[]> Written { get; } = [];

        public byte[]? Get( string key ) => __inner.Get(key);
        public Task<byte[]?> GetAsync( string key, CancellationToken token = default ) => __inner.GetAsync(key, token);
        public void Refresh( string key ) => __inner.Refresh(key);
        public Task RefreshAsync( string key, CancellationToken token = default ) => __inner.RefreshAsync(key, token);
        public void Remove( string key ) => __inner.Remove(key);
        public Task RemoveAsync( string key, CancellationToken token = default ) => __inner.RemoveAsync(key, token);

        public void Set( string key, byte[] value, DistributedCacheEntryOptions options )
        {
            Written.Add(value);
            __inner.Set(key, value, options);
        }

        public Task SetAsync( string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default )
        {
            Written.Add(value);
            return __inner.SetAsync(key, value, options, token);
        }
    }


    /// <summary> A stand-in for FusionCache's System.Text.Json serializer. </summary>
    private sealed class RecordingFallback : IFusionCacheSerializer
    {
        public int Serialized   { get; private set; }
        public int Deserialized { get; private set; }

        public byte[] Serialize<T>( T? obj )
        {
            Serialized++;
            return JsonSerializer.SerializeToUtf8Bytes(obj);
        }

        public T? Deserialize<T>( byte[] data )
        {
            Deserialized++;
            return JsonSerializer.Deserialize<T>(data);
        }

        public ValueTask<byte[]> SerializeAsync<T>( T?    obj,  CancellationToken token = default ) => ValueTask.FromResult(Serialize(obj));
        public ValueTask<T?>     DeserializeAsync<T>( byte[] data, CancellationToken token = default ) => ValueTask.FromResult(Deserialize<T>(data));
    }
}
