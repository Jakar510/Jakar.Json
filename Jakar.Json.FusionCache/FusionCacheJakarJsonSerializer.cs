// Jakar.Json.FusionCache
// 10/17/2026

namespace Jakar.Json.FusionCache;


/// <summary>
///     FusionCache's distributed-cache serializer on Jakar.Json: <c>services.AddFusionCache().WithJakarJsonSerializer()</c>.
///     <para>
///         FusionCache stores each value inside a <see cref="FusionCacheDistributedEntry{TValue}"/>. The entry (timestamps, tags, metadata) is written as a compact binary envelope; the
///         value inside it is written by Jakar.Json when it's a <c>[JsonModel]</c> type or a collection of one (anything <see cref="JsonCodecRegistry"/> resolves), and in binary when it's a
///         common scalar (<see cref="string"/>, <see cref="bool"/>, the integer and floating-point types, <see cref="decimal"/>, <see cref="char"/>, <see cref="Guid"/>, the date and time types,
///         enums and <c>byte[]</c>).
///     </para>
///     <para>
///         Any other type goes to <see cref="Fallback"/> when one is set, otherwise serializing it throws <see cref="NotSupportedException"/>. Reading checks the envelope's header, so payloads
///         the fallback wrote (or that were written before switching to this serializer, by the same serializer as the fallback) are read back by the fallback.
///     </para>
/// </summary>
/// <remarks>
///     Native AOT: entries are recognized without reflection, but their per-value-type codec is created with <see cref="System.Reflection.MethodInfo.MakeGenericMethod"/> when dynamic code is
///     available. Under Native AOT, call <see cref="JakarJsonFusionCache.Register{TValue}"/> at startup for every value type the cache stores.
/// </remarks>
/// <param name="fallback"> Serializes the types Jakar.Json can't (for example <c>FusionCacheSystemTextJsonSerializer</c>), and reads the payloads it wrote. </param>
public sealed class FusionCacheJakarJsonSerializer( IFusionCacheSerializer? fallback = null ) : IFusionCacheSerializer
{
    /// <summary> Handles the types this serializer can't, and the payloads they were written to. </summary>
    public IFusionCacheSerializer? Fallback { get; } = fallback;


    /// <summary> Whether <typeparamref name="T"/> (a value, or a <see cref="FusionCacheDistributedEntry{TValue}"/> of one) is written by this serializer rather than <see cref="Fallback"/>. </summary>
    public static bool IsSupported<T>() => PayloadCodecs<T>.Codec is not null;


    public byte[] Serialize<T>( T? obj ) => PayloadCodecs<T>.Codec is { } codec
                                                ? codec.Serialize(obj)
                                                : Fallback?.Serialize(obj) ?? throw Unsupported(typeof(T));

    public T? Deserialize<T>( byte[] data )
    {
        ArgumentNullException.ThrowIfNull(data);

        if ( Envelope.IsEnvelope(data) )
        {
            return PayloadCodecs<T>.Codec is { } codec
                       ? codec.Deserialize(data)
                       : throw Unsupported(typeof(T));
        }

        return Fallback is not null
                   ? Fallback.Deserialize<T>(data)
                   : throw new FusionCacheSerializationException($"The payload wasn't written by {nameof(FusionCacheJakarJsonSerializer)} and no fallback serializer is set");
    }

    public ValueTask<byte[]> SerializeAsync<T>( T? obj, CancellationToken token = default ) => PayloadCodecs<T>.Codec is { } codec
                                                                                                  ? ValueTask.FromResult(codec.Serialize(obj))
                                                                                                  : Fallback?.SerializeAsync(obj, token) ?? ValueTask.FromException<byte[]>(Unsupported(typeof(T)));

    public ValueTask<T?> DeserializeAsync<T>( byte[] data, CancellationToken token = default )
    {
        ArgumentNullException.ThrowIfNull(data);

        if ( Fallback is not null && !Envelope.IsEnvelope(data) ) { return Fallback.DeserializeAsync<T>(data, token); }

        try { return ValueTask.FromResult(Deserialize<T>(data)); }
        catch ( Exception e ) { return ValueTask.FromException<T?>(e); }
    }


    private static NotSupportedException Unsupported( Type type ) => new($"{nameof(FusionCacheJakarJsonSerializer)} can't serialize {type}: the value must be a [JsonModel] type, a collection of one, or a supported scalar. Set a fallback serializer for other types; under Native AOT, also call {nameof(JakarJsonFusionCache)}.{nameof(JakarJsonFusionCache.Register)}<TValue>() for each cached value type.");
}
