// Jakar.Json.StackExchangeRedis.Tests
// 10/17/2026

using System.Reflection;
using System.Text;
using StackExchange.Redis;
using Order = Jakar.Json.Tests.Shared.Order;



namespace Jakar.Json.StackExchangeRedis.Tests;


/// <summary> Behavior against an in-memory stand-in for <see cref="IDatabase"/> (the commands these helpers send: SET/GET and HSET/HGET, with leases). </summary>
[TestFixture]
public sealed class RedisTests
{
    private FakeRedis __redis    = null!;
    private IDatabase __database = null!;


    [SetUp] public void Create() => ( __database, __redis ) = FakeRedis.Create();


    [Test] public async Task Strings_RoundTrip_SyncAndAsync()
    {
        await __database.StringSetJakarJsonAsync("order", Samples.Order(), TimeSpan.FromMinutes(1));
        __database.StringSetJakarJson("orders", Samples.Orders());
        __database.StringSetJakarJson("point", new Point { X = 1, Y = 2 });

        await Assert.MultipleAsync(async () =>
                                   {
                                       Assert.That(await __database.StringGetJakarJsonAsync<Order>("order"),        Is.EqualTo(Samples.Order()));
                                       Assert.That(__database.StringGetJakarJson<List<Order>>("orders"),            Is.EqualTo(Samples.Orders()));
                                       Assert.That(__database.StringGetJakarJson<Point>("point"),                   Is.EqualTo(new Point { X = 1, Y = 2 }));
                                       Assert.That(__redis.Expiries["order"],                                       Is.EqualTo(TimeSpan.FromMinutes(1)));
                                       Assert.That(Encoding.UTF8.GetString(__redis.Strings["order"]),               Does.Contain("\"customer\" : \"Zoë\""));
                                   });
    }

    [Test] public async Task Hashes_RoundTrip_SyncAndAsync()
    {
        await __database.HashSetJakarJsonAsync("customer:7", "latest", Samples.Order());
        __database.HashSetJakarJson("customer:7", "all", Samples.Orders());

        await Assert.MultipleAsync(async () =>
                                   {
                                       Assert.That(await __database.HashGetJakarJsonAsync<Order>("customer:7", "latest"), Is.EqualTo(Samples.Order()));
                                       Assert.That(__database.HashGetJakarJson<List<Order>>("customer:7", "all"),         Is.EqualTo(Samples.Orders()));
                                   });
    }

    [Test] public async Task Missing_IsDefault()
    {
        await Assert.MultipleAsync(async () =>
                                   {
                                       Assert.That(await __database.StringGetJakarJsonAsync<Order>("missing"),        Is.Null);
                                       Assert.That(__database.HashGetJakarJson<Order>("missing", "field"),            Is.Null);
                                       Assert.That(__database.StringGetJakarJson<Point>("missing"),                   Is.EqualTo(default(Point)));
                                   });
    }

    [Test] public void NormalCommands_SendThePooledBuffer()
    {
        __database.StringSetJakarJson("order", Samples.Order());
        Assert.That(__redis.LastValueWasArrayBacked, Is.False, "no intermediate byte[]: the command completes before the buffer is returned");
    }

    [Test] public void FireAndForget_SendsACopy()
    {
        __database.StringSetJakarJson("order", Samples.Order(), flags: CommandFlags.FireAndForget);
        Assert.That(__redis.LastValueWasArrayBacked, Is.True, "the pooled buffer is returned before a fire-and-forget command is sent");
    }

    [Test] public void Corrupt_Throws()
    {
        __redis.Strings["bad"] = "{\"id\":"u8.ToArray();
        Assert.Throws<JsonReadException>(() => __database.StringGetJakarJson<Order>("bad"));
    }

    [Test] public void Unsupported_Throws() => Assert.Throws<NotSupportedException>(() => __database.StringSetJakarJson("x", new PlainDto("x")));
}



/// <summary> An in-memory <see cref="IDatabase"/> implementing just the commands the helpers use. </summary>
public class FakeRedis : DispatchProxy
{
    public Dictionary<string, byte[]>    Strings  { get; } = [];
    public Dictionary<string, TimeSpan?> Expiries { get; } = [];
    public Dictionary<string, byte[]>    Hashes   { get; } = [];
    public bool                          LastValueWasArrayBacked { get; private set; }


    public static (IDatabase Database, FakeRedis Fake) Create()
    {
        IDatabase database = DispatchProxy.Create<IDatabase, FakeRedis>();
        return ( database, (FakeRedis)(object)database );
    }


    protected override object? Invoke( MethodInfo? method, object?[]? args )
    {
        args ??= [];

        switch ( method!.Name )
        {
            case nameof(IDatabase.StringSet) or nameof(IDatabaseAsync.StringSetAsync):
            {
                string key = ((RedisKey)args[0]!).ToString();
                Strings[key]  = Store((RedisValue)args[1]!);
                Expiries[key] = args[2] as TimeSpan?;
                return Result(method, true);
            }

            case nameof(IDatabase.StringGetLease) or nameof(IDatabaseAsync.StringGetLeaseAsync):
                return Result(method, Lease(Strings.GetValueOrDefault(((RedisKey)args[0]!).ToString())));

            case nameof(IDatabase.HashSet) or nameof(IDatabaseAsync.HashSetAsync):
                Hashes[$"{((RedisKey)args[0]!).ToString()}/{((RedisValue)args[1]!).ToString()}"] = Store((RedisValue)args[2]!);
                return Result(method, true);

            case nameof(IDatabase.HashGetLease) or nameof(IDatabaseAsync.HashGetLeaseAsync):
                return Result(method, Lease(Hashes.GetValueOrDefault($"{((RedisKey)args[0]!).ToString()}/{((RedisValue)args[1]!).ToString()}")));

            default: throw new NotSupportedException(method.Name);
        }
    }


    private byte[] Store( RedisValue value )
    {
        // A copy is an array of exactly the payload's length; the pooled buffer is a larger rented array.
        ReadOnlyMemory<byte> memory = value;
        LastValueWasArrayBacked = System.Runtime.InteropServices.MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> segment) && segment.Offset == 0 && segment.Array!.Length == segment.Count;
        return memory.ToArray();
    }

    private static Lease<byte>? Lease( byte[]? data )
    {
        if ( data is null ) { return null; }

        Lease<byte> lease = Lease<byte>.Create(data.Length, clear: false);
        data.CopyTo(lease.Span);
        return lease;
    }

    private static object? Result<T>( MethodInfo method, T value ) => method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
                                                                          ? Task.FromResult(value)
                                                                          : value;
}
