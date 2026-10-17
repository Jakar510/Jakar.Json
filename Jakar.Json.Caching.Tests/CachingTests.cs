// Jakar.Json.Caching.Tests
// 10/17/2026

using System.Buffers;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;



namespace Jakar.Json.Caching.Tests;


[TestFixture]
public sealed class HybridCacheTests
{
    // ─── Unit ────────────────────────────────────────────────────────────────

    [Test] public void Factory_CreatesSerializersOnlyForModels()
    {
        JakarJsonHybridCacheSerializerFactory factory = new();

        Assert.Multiple(() =>
                        {
                            Assert.That(factory.TryCreateSerializer(out IHybridCacheSerializer<Order>? order),        Is.True);
                            Assert.That(order,                                                                      Is.InstanceOf<JakarJsonHybridCacheSerializer<Order>>());
                            Assert.That(factory.TryCreateSerializer(out IHybridCacheSerializer<List<Order>>? _),      Is.True);
                            Assert.That(factory.TryCreateSerializer(out IHybridCacheSerializer<Point>? _),            Is.True);
                            Assert.That(factory.TryCreateSerializer(out IHybridCacheSerializer<PlainDto>? plain),     Is.False);
                            Assert.That(plain,                                                                      Is.Null);
                            Assert.That(factory.TryCreateSerializer(out IHybridCacheSerializer<string>? _),           Is.False, "HybridCache handles strings and byte[] itself");
                        });
    }

    [Test] public void Serializer_RoundTrips_AcrossSegments()
    {
        JakarJsonHybridCacheSerializer<Order> serializer = new();
        ArrayBufferWriter<byte>              buffer     = new();
        serializer.Serialize(Samples.Order(), buffer);

        byte[] bytes = buffer.WrittenSpan.ToArray();

        Assert.Multiple(() =>
                        {
                            Assert.That(Encoding.UTF8.GetString(bytes),                                    Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(serializer.Deserialize(new ReadOnlySequence<byte>(bytes)),          Is.EqualTo(Samples.Order()));
                            Assert.That(serializer.Deserialize(Segments.Split(bytes, 3)),                   Is.EqualTo(Samples.Order()), "a payload split across segments");
                        });
    }

    [Test] public void Serializer_ForUnsupportedType_Throws() => Assert.Throws<NotSupportedException>(() => _ = new JakarJsonHybridCacheSerializer<PlainDto>());


    // ─── Integration: HybridCache with a shared L2 ───────────────────────────

    [Test] public async Task HybridCache_SharesModelsThroughTheDistributedCache()
    {
        RecordingDistributedCache l2 = new();

        await using ServiceProvider writer = Provider(l2);
        await using ServiceProvider reader = Provider(l2); // its own L1, so it has to read the L2 payload

        await writer.GetRequiredService<HybridCache>().SetAsync("order:7", Samples.Order());
        await writer.GetRequiredService<HybridCache>().SetAsync("orders", Samples.Orders());

        Order       order  = await reader.GetRequiredService<HybridCache>().GetOrCreateAsync("order:7", static _ => ValueTask.FromResult(new Order { Id = -1 }));
        List<Order> orders = await reader.GetRequiredService<HybridCache>().GetOrCreateAsync("orders",  static _ => ValueTask.FromResult(new List<Order>()));

        Assert.Multiple(() =>
                        {
                            Assert.That(order,  Is.EqualTo(Samples.Order()), "read from L2, not created");
                            Assert.That(orders, Is.EqualTo(Samples.Orders()));
                            Assert.That(l2.Written.Select(static x => Encoding.UTF8.GetString(x)), Has.All.Contains(Samples.JAKAR_SEPARATOR), "stored by Jakar.Json");
                        });
    }

    [Test] public async Task HybridCache_OtherTypes_KeepTheDefaultSerializer()
    {
        RecordingDistributedCache l2 = new();

        await using ServiceProvider writer = Provider(l2);
        await using ServiceProvider reader = Provider(l2);

        await writer.GetRequiredService<HybridCache>().SetAsync("plain", new PlainDto("x"));
        PlainDto plain = await reader.GetRequiredService<HybridCache>().GetOrCreateAsync("plain", static _ => ValueTask.FromResult(new PlainDto("created")));

        Assert.Multiple(() =>
                        {
                            Assert.That(plain,                                              Is.EqualTo(new PlainDto("x")));
                            Assert.That(Encoding.UTF8.GetString(l2.Written.Single()),       Does.Not.Contain(Samples.JAKAR_SEPARATOR), "System.Text.Json, HybridCache's default");
                        });
    }


    private static ServiceProvider Provider( IDistributedCache l2 )
    {
        ServiceCollection services = new();
        services.AddSingleton(l2);
        services.AddHybridCache().AddJakarJsonSerializer();
        return services.BuildServiceProvider();
    }
}



[TestFixture]
public sealed class DistributedCacheTests
{
    private static IEnumerable<TestCaseData> Caches()
    {
        yield return new TestCaseData(new RecordingDistributedCache()).SetArgDisplayNames("IDistributedCache");
        yield return new TestCaseData(new BufferDistributedCache()).SetArgDisplayNames("IBufferDistributedCache");
    }


    [TestCaseSource(nameof(Caches))] public async Task Models_RoundTrip( RecordingDistributedCache cache )
    {
        await cache.SetJakarJsonAsync("order", Samples.Order());
        cache.SetJakarJson("orders", Samples.Orders(), new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(1) });
        cache.SetJakarJson("point", new Point { X = 3, Y = 4 });

        await Assert.MultipleAsync(async () =>
                        {
                            Assert.That(await cache.GetJakarJsonAsync<Order>("order"),  Is.EqualTo(Samples.Order()));
                            Assert.That(cache.GetJakarJson<Order>("order"),             Is.EqualTo(Samples.Order()));
                            Assert.That(await cache.GetJakarJsonAsync<List<Order>>("orders"), Is.EqualTo(Samples.Orders()));
                            Assert.That(cache.GetJakarJson<Point>("point"),             Is.EqualTo(new Point { X = 3, Y = 4 }));
                            Assert.That(Encoding.UTF8.GetString(cache.Get("order")!),   Does.Contain("\"customer\" : \"Zoë\""), "stored as Jakar.Json UTF-8");
                        });
    }

    [TestCaseSource(nameof(Caches))] public async Task Missing_IsDefault( RecordingDistributedCache cache )
    {
        await Assert.MultipleAsync(async () =>
                        {
                            Assert.That(await cache.GetJakarJsonAsync<Order>("missing"), Is.Null);
                            Assert.That(cache.GetJakarJson<Point>("missing"),            Is.EqualTo(default(Point)));
                        });
    }

    [Test] public void BufferCache_UsesTheBufferApi()
    {
        BufferDistributedCache cache = new();
        cache.SetJakarJson("order", Samples.Order());
        _ = cache.GetJakarJson<Order>("order");

        Assert.Multiple(() =>
                        {
                            Assert.That(cache.BufferSets, Is.EqualTo(1));
                            Assert.That(cache.BufferGets, Is.EqualTo(1));
                            Assert.That(cache.ArraySets,  Is.Zero);
                        });
    }

    [Test] public void Corrupt_Throws()
    {
        RecordingDistributedCache cache = new();
        cache.Set("order", "{\"id\":"u8.ToArray(), new DistributedCacheEntryOptions());
        Assert.Throws<JsonReadException>(() => cache.GetJakarJson<Order>("order"));
    }

    [Test] public async Task Unsupported_Throws() => await Assert.ThrowsAsync<NotSupportedException>(() => new RecordingDistributedCache().SetJakarJsonAsync("x", new PlainDto("x")));
}



/// <summary> An in-memory <see cref="IDistributedCache"/> that keeps what was written to it. </summary>
public class RecordingDistributedCache : IDistributedCache
{
    protected readonly MemoryDistributedCache Inner = new(Options.Create(new MemoryDistributedCacheOptions()));

    public List<byte[]> Written   { get; } = [];
    public int          ArraySets { get; private set; }

    public byte[]?       Get( string key ) => Inner.Get(key);
    public Task<byte[]?> GetAsync( string key, CancellationToken token = default ) => Inner.GetAsync(key, token);
    public void          Refresh( string key ) => Inner.Refresh(key);
    public Task          RefreshAsync( string key, CancellationToken token = default ) => Inner.RefreshAsync(key, token);
    public void          Remove( string key ) => Inner.Remove(key);
    public Task          RemoveAsync( string key, CancellationToken token = default ) => Inner.RemoveAsync(key, token);

    public void Set( string key, byte[] value, DistributedCacheEntryOptions options )
    {
        ArraySets++;
        Written.Add(value);
        Inner.Set(key, value, options);
    }

    public Task SetAsync( string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default )
    {
        ArraySets++;
        Written.Add(value);
        return Inner.SetAsync(key, value, options, token);
    }
}



/// <summary> Adds the buffer API, as Redis and SQL Server caches do in .NET 9+. </summary>
public sealed class BufferDistributedCache : RecordingDistributedCache, IBufferDistributedCache
{
    public int BufferSets { get; private set; }
    public int BufferGets { get; private set; }

    public bool TryGet( string key, IBufferWriter<byte> destination )
    {
        BufferGets++;
        if ( Inner.Get(key) is not { } data ) { return false; }

        destination.Write(data);
        return true;
    }

    public ValueTask<bool> TryGetAsync( string key, IBufferWriter<byte> destination, CancellationToken token = default ) => ValueTask.FromResult(TryGet(key, destination));

    public void Set( string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options )
    {
        BufferSets++;
        Inner.Set(key, value.ToArray(), options);
    }

    public ValueTask SetAsync( string key, ReadOnlySequence<byte> value, DistributedCacheEntryOptions options, CancellationToken token = default )
    {
        Set(key, value, options);
        return ValueTask.CompletedTask;
    }
}
