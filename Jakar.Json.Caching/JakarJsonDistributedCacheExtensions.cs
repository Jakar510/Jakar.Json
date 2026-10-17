// Jakar.Json.Caching
// 10/17/2026

using Microsoft.Extensions.Caching.Distributed;



namespace Jakar.Json.Caching;


/// <summary>
///     Stores <c>[JsonModel]</c> values (and arrays, lists and collection interfaces of them) in an <see cref="IDistributedCache"/> as Jakar.Json UTF-8. When the cache implements
///     <see cref="IBufferDistributedCache"/> (Redis, SQL Server and others in .NET 9+), values are written and read through pooled buffers with no intermediate <c>byte[]</c>.
/// </summary>
public static class JakarJsonDistributedCacheExtensions
{
    /// <summary> The value stored under <paramref name="key"/>, or <see langword="default"/> when there's none. </summary>
    /// <exception cref="NotSupportedException"> Jakar.Json can't serialize <typeparamref name="T"/>. </exception>
    /// <exception cref="JsonReadException"> The stored value isn't valid JSON for <typeparamref name="T"/>. </exception>
    public static T? GetJakarJson<T>( this IDistributedCache cache, string key )
    {
        ArgumentNullException.ThrowIfNull(cache);
        JsonTypeCodec codec = JsonModels<T>.RequiredCodec;

        if ( cache is IBufferDistributedCache buffered )
        {
            using PooledBufferWriter buffer = new();
            return buffered.TryGet(key, buffer)
                       ? (T?)codec.Read(buffer.WrittenSpan)
                       : default;
        }

        return cache.Get(key) is { } data
                   ? (T?)codec.Read(data)
                   : default;
    }

    /// <inheritdoc cref="GetJakarJson{T}"/>
    public static async Task<T?> GetJakarJsonAsync<T>( this IDistributedCache cache, string key, CancellationToken token = default )
    {
        ArgumentNullException.ThrowIfNull(cache);
        JsonTypeCodec codec = JsonModels<T>.RequiredCodec;

        if ( cache is IBufferDistributedCache buffered )
        {
            using PooledBufferWriter buffer = new();
            return await buffered.TryGetAsync(key, buffer, token).ConfigureAwait(false)
                       ? (T?)codec.Read(buffer.WrittenSpan)
                       : default;
        }

        return await cache.GetAsync(key, token).ConfigureAwait(false) is { } data
                   ? (T?)codec.Read(data)
                   : default;
    }

    /// <summary> Stores <paramref name="value"/> under <paramref name="key"/>. </summary>
    /// <exception cref="NotSupportedException"> Jakar.Json can't serialize <typeparamref name="T"/>. </exception>
    public static void SetJakarJson<T>( this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions? options = null )
    {
        ArgumentNullException.ThrowIfNull(cache);
        options ??= new DistributedCacheEntryOptions();

        using PooledBufferWriter buffer = new();
        JsonModels<T>.Write(value, buffer);

        if ( cache is IBufferDistributedCache buffered ) { buffered.Set(key, new ReadOnlySequence<byte>(buffer.WrittenMemory), options); }
        else { cache.Set(key, buffer.ToArray(), options); }
    }

    /// <inheritdoc cref="SetJakarJson{T}"/>
    public static async Task SetJakarJsonAsync<T>( this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions? options = null, CancellationToken token = default )
    {
        ArgumentNullException.ThrowIfNull(cache);
        options ??= new DistributedCacheEntryOptions();

        using PooledBufferWriter buffer = new();
        JsonModels<T>.Write(value, buffer);

        if ( cache is IBufferDistributedCache buffered ) { await buffered.SetAsync(key, new ReadOnlySequence<byte>(buffer.WrittenMemory), options, token).ConfigureAwait(false); }
        else { await cache.SetAsync(key, buffer.ToArray(), options, token).ConfigureAwait(false); }
    }
}
