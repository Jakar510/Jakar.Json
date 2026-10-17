// Jakar.Json.FusionCache
// 10/17/2026

using System.Reflection;



namespace Jakar.Json.FusionCache;


/// <summary> Registers <see cref="FusionCacheJakarJsonSerializer"/> with FusionCache, and the value types it stores under Native AOT. </summary>
public static class JakarJsonFusionCache
{
    private static readonly ConcurrentDictionary<Type, object> __entries = new();


    /// <summary> Uses <see cref="FusionCacheJakarJsonSerializer"/> for this cache: <c>services.AddFusionCache().WithJakarJsonSerializer()</c>. </summary>
    /// <param name="builder"> The cache being configured. </param>
    /// <param name="fallback"> Serializes the types Jakar.Json can't, and reads the payloads it wrote. </param>
    public static IFusionCacheBuilder WithJakarJsonSerializer( this IFusionCacheBuilder builder, IFusionCacheSerializer? fallback = null )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithSerializer(new FusionCacheJakarJsonSerializer(fallback));
    }


    /// <summary>
    ///     Makes the codec for <see cref="FusionCacheDistributedEntry{TValue}"/> available without dynamic code. Needed only under Native AOT (elsewhere it's created on first use); call it at
    ///     startup, before the cache reads or writes a <typeparamref name="TValue"/>, once for every value type the cache stores.
    /// </summary>
    /// <exception cref="NotSupportedException"> <typeparamref name="TValue"/> isn't a <c>[JsonModel]</c> type, a collection of one, or a supported scalar. </exception>
    public static void Register<TValue>()
    {
        if ( !ValueCodec<TValue>.IsSupported ) { throw new NotSupportedException($"{typeof(TValue)} isn't a [JsonModel] type, a collection of one, or a supported scalar"); }

        __entries.TryAdd(typeof(FusionCacheDistributedEntry<TValue>), new EntryCodec<TValue>());
    }


    /// <summary> The codec for <paramref name="entryType"/> (a <see cref="FusionCacheDistributedEntry{TValue}"/>): registered, or created when dynamic code is available. </summary>
    internal static object? GetEntryCodec( Type entryType )
    {
        if ( __entries.TryGetValue(entryType, out object? registered) ) { return registered; }

        if ( !RuntimeFeature.IsDynamicCodeSupported ) { return null; }

        return CreateEntryCodec(entryType.GetGenericArguments()[0]);
    }

    [RequiresDynamicCode("Creates EntryCodec<TValue> for a value type known only at run time; under Native AOT, Register<TValue>() is used instead")]
    private static object? CreateEntryCodec( Type valueType ) => typeof(JakarJsonFusionCache).GetMethod(nameof(CreateEntryCodecCore), BindingFlags.NonPublic | BindingFlags.Static)!
                                                                                             .MakeGenericMethod(valueType)
                                                                                             .Invoke(null, null);

    private static object? CreateEntryCodecCore<TValue>() => ValueCodec<TValue>.IsSupported
                                                                 ? new EntryCodec<TValue>()
                                                                 : null;
}
