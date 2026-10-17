// Jakar.Json.Kafka
// 10/17/2026

using Confluent.Kafka;



namespace Jakar.Json.Kafka;


/// <summary>
///     Writes and reads Kafka message keys or values of <typeparamref name="T"/> (a <c>[JsonModel]</c> type, or an array, list or collection interface of one) as Jakar.Json UTF-8. A
///     <see langword="null"/> value is a Kafka null (a tombstone), and a null message reads as <see langword="default"/>.
/// </summary>
/// <exception cref="NotSupportedException"> Jakar.Json can't serialize <typeparamref name="T"/>. </exception>
public sealed class JakarJsonKafkaSerializer<T>() : ISerializer<T>, IAsyncSerializer<T>, IDeserializer<T>, IAsyncDeserializer<T>
{
    private readonly JsonTypeCodec __codec = JsonModels<T>.RequiredCodec;


    public byte[] Serialize( T data, SerializationContext context )
    {
        if ( data is null ) { return null!; } // Kafka's null value

        using PooledBufferWriter buffer = new();
        __codec.Write(data, buffer);
        return buffer.ToArray();
    }

    public Task<byte[]> SerializeAsync( T data, SerializationContext context ) => Task.FromResult(Serialize(data, context));

    public T Deserialize( ReadOnlySpan<byte> data, bool isNull, SerializationContext context ) => isNull
                                                                                                     ? default!
                                                                                                     : (T)__codec.Read(data)!;

    public Task<T> DeserializeAsync( ReadOnlyMemory<byte> data, bool isNull, SerializationContext context ) => Task.FromResult(Deserialize(data.Span, isNull, context));
}



public static class JakarJsonKafkaBuilderExtensions
{
    /// <summary> Writes message values with Jakar.Json. </summary>
    public static ProducerBuilder<TKey, TValue> SetJakarJsonValueSerializer<TKey, TValue>( this ProducerBuilder<TKey, TValue> builder ) => builder.SetValueSerializer((ISerializer<TValue>)new JakarJsonKafkaSerializer<TValue>());

    /// <summary> Writes message keys with Jakar.Json. </summary>
    public static ProducerBuilder<TKey, TValue> SetJakarJsonKeySerializer<TKey, TValue>( this ProducerBuilder<TKey, TValue> builder ) => builder.SetKeySerializer((ISerializer<TKey>)new JakarJsonKafkaSerializer<TKey>());

    /// <summary> Reads message values with Jakar.Json. </summary>
    public static ConsumerBuilder<TKey, TValue> SetJakarJsonValueDeserializer<TKey, TValue>( this ConsumerBuilder<TKey, TValue> builder ) => builder.SetValueDeserializer(new JakarJsonKafkaSerializer<TValue>());

    /// <summary> Reads message keys with Jakar.Json. </summary>
    public static ConsumerBuilder<TKey, TValue> SetJakarJsonKeyDeserializer<TKey, TValue>( this ConsumerBuilder<TKey, TValue> builder ) => builder.SetKeyDeserializer(new JakarJsonKafkaSerializer<TKey>());
}
