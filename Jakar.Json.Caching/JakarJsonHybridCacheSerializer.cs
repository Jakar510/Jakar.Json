// Jakar.Json.Caching
// 10/17/2026

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;



namespace Jakar.Json.Caching;


/// <summary>
///     Gives HybridCache a Jakar.Json serializer for every <c>[JsonModel]</c> type (and arrays, lists and collection interfaces of them). It declines every other type, so HybridCache keeps
///     its default serializer for those: <c>services.AddHybridCache().AddJakarJsonSerializer()</c>.
/// </summary>
public sealed class JakarJsonHybridCacheSerializerFactory : IHybridCacheSerializerFactory
{
    public bool TryCreateSerializer<T>( [NotNullWhen(true)] out IHybridCacheSerializer<T>? serializer )
    {
        serializer = JsonModels<T>.IsSupported
                         ? new JakarJsonHybridCacheSerializer<T>()
                         : null;

        return serializer is not null;
    }
}



/// <summary> Writes <typeparamref name="T"/> straight into HybridCache's buffer with Jakar.Json, and reads it back from the (possibly multi-segment) payload. </summary>
/// <exception cref="NotSupportedException"> Jakar.Json can't serialize <typeparamref name="T"/>. </exception>
public sealed class JakarJsonHybridCacheSerializer<T>() : IHybridCacheSerializer<T>
{
    private readonly JsonTypeCodec __codec = JsonModels<T>.RequiredCodec;


    public void Serialize( T value, IBufferWriter<byte> target ) => __codec.Write(value, target);

    public T Deserialize( ReadOnlySequence<byte> source ) => (T)__codec.Read(source)!;
}



public static class JakarJsonHybridCacheBuilderExtensions
{
    /// <summary> Serializes <c>[JsonModel]</c> types with Jakar.Json; HybridCache's default serializer keeps every other type. </summary>
    public static IHybridCacheBuilder AddJakarJsonSerializer( this IHybridCacheBuilder builder )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddSerializerFactory(new JakarJsonHybridCacheSerializerFactory());
    }
}
