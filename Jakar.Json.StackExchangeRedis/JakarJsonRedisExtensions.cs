// Jakar.Json.StackExchangeRedis
// 10/17/2026

using StackExchange.Redis;



namespace Jakar.Json.StackExchangeRedis;


/// <summary>
///     Stores <c>[JsonModel]</c> values (and arrays, lists and collection interfaces of them) in Redis strings and hash fields as Jakar.Json UTF-8. Values are written from pooled buffers
///     (copied instead when the command is fire-and-forget, since the buffer would be returned before it's sent) and read from pooled leases, so neither direction allocates a byte array.
/// </summary>
public static class JakarJsonRedisExtensions
{
    // ─── Strings ─────────────────────────────────────────────────────────────

    /// <summary> <c>SET key value</c> with the value written by Jakar.Json. </summary>
    /// <exception cref="NotSupportedException"> Jakar.Json can't serialize <typeparamref name="T"/>. </exception>
    public static async Task<bool> StringSetJakarJsonAsync<T>( this IDatabaseAsync database, RedisKey key, T value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None )
    {
        ArgumentNullException.ThrowIfNull(database);
        using PooledBufferWriter buffer = Write(value);
        return await database.StringSetAsync(key, Value(buffer, flags), expiry, when, flags).ConfigureAwait(false);
    }

    /// <inheritdoc cref="StringSetJakarJsonAsync{T}"/>
    public static bool StringSetJakarJson<T>( this IDatabase database, RedisKey key, T value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None )
    {
        ArgumentNullException.ThrowIfNull(database);
        using PooledBufferWriter buffer = Write(value);
        return database.StringSet(key, Value(buffer, flags), expiry, when, flags);
    }

    /// <summary> <c>GET key</c>, read by Jakar.Json; <see langword="default"/> when the key doesn't exist. </summary>
    /// <exception cref="JsonReadException"> The stored value isn't valid JSON for <typeparamref name="T"/>. </exception>
    public static async Task<T?> StringGetJakarJsonAsync<T>( this IDatabaseAsync database, RedisKey key, CommandFlags flags = CommandFlags.None )
    {
        ArgumentNullException.ThrowIfNull(database);
        JsonTypeCodec codec = JsonModels<T>.RequiredCodec;
        using Lease<byte>? lease = await database.StringGetLeaseAsync(key, flags).ConfigureAwait(false);
        return Read<T>(codec, lease);
    }

    /// <inheritdoc cref="StringGetJakarJsonAsync{T}"/>
    public static T? StringGetJakarJson<T>( this IDatabase database, RedisKey key, CommandFlags flags = CommandFlags.None )
    {
        ArgumentNullException.ThrowIfNull(database);
        JsonTypeCodec      codec = JsonModels<T>.RequiredCodec;
        using Lease<byte>? lease = database.StringGetLease(key, flags);
        return Read<T>(codec, lease);
    }


    // ─── Hashes ──────────────────────────────────────────────────────────────

    /// <summary> <c>HSET key field value</c> with the value written by Jakar.Json. </summary>
    /// <exception cref="NotSupportedException"> Jakar.Json can't serialize <typeparamref name="T"/>. </exception>
    public static async Task<bool> HashSetJakarJsonAsync<T>( this IDatabaseAsync database, RedisKey key, RedisValue field, T value, When when = When.Always, CommandFlags flags = CommandFlags.None )
    {
        ArgumentNullException.ThrowIfNull(database);
        using PooledBufferWriter buffer = Write(value);
        return await database.HashSetAsync(key, field, Value(buffer, flags), when, flags).ConfigureAwait(false);
    }

    /// <inheritdoc cref="HashSetJakarJsonAsync{T}"/>
    public static bool HashSetJakarJson<T>( this IDatabase database, RedisKey key, RedisValue field, T value, When when = When.Always, CommandFlags flags = CommandFlags.None )
    {
        ArgumentNullException.ThrowIfNull(database);
        using PooledBufferWriter buffer = Write(value);
        return database.HashSet(key, field, Value(buffer, flags), when, flags);
    }

    /// <summary> <c>HGET key field</c>, read by Jakar.Json; <see langword="default"/> when the field doesn't exist. </summary>
    public static async Task<T?> HashGetJakarJsonAsync<T>( this IDatabaseAsync database, RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None )
    {
        ArgumentNullException.ThrowIfNull(database);
        JsonTypeCodec codec = JsonModels<T>.RequiredCodec;
        using Lease<byte>? lease = await database.HashGetLeaseAsync(key, field, flags).ConfigureAwait(false);
        return Read<T>(codec, lease);
    }

    /// <inheritdoc cref="HashGetJakarJsonAsync{T}"/>
    public static T? HashGetJakarJson<T>( this IDatabase database, RedisKey key, RedisValue field, CommandFlags flags = CommandFlags.None )
    {
        ArgumentNullException.ThrowIfNull(database);
        JsonTypeCodec      codec = JsonModels<T>.RequiredCodec;
        using Lease<byte>? lease = database.HashGetLease(key, field, flags);
        return Read<T>(codec, lease);
    }


    private static PooledBufferWriter Write<T>( T value )
    {
        PooledBufferWriter buffer = new();

        try { JsonModels<T>.Write(value, buffer); }
        catch
        {
            buffer.Dispose();
            throw;
        }

        return buffer;
    }

    /// <summary> The pooled bytes themselves, unless the command returns before it's sent (fire-and-forget): then a copy. </summary>
    private static RedisValue Value( PooledBufferWriter buffer, CommandFlags flags ) => ( flags & CommandFlags.FireAndForget ) != 0
                                                                                           ? buffer.ToArray()
                                                                                           : buffer.WrittenMemory;

    private static T? Read<T>( JsonTypeCodec codec, Lease<byte>? lease ) => lease is null
                                                                                ? default
                                                                                : (T?)codec.Read(lease.Span);
}
