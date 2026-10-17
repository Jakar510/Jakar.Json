# Jakar.Json.Kafka

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) serializers for [Confluent.Kafka](https://github.com/confluentinc/confluent-kafka-dotnet).

```csharp
using IProducer<string, Order> producer = new ProducerBuilder<string, Order>(config)
    .SetJakarJsonValueSerializer()
    .Build();

using IConsumer<string, Order> consumer = new ConsumerBuilder<string, Order>(consumerConfig)
    .SetJakarJsonValueDeserializer()
    .Build();
```

- **`JakarJsonKafkaSerializer<T>`** implements `ISerializer<T>`, `IAsyncSerializer<T>`, `IDeserializer<T>` and `IAsyncDeserializer<T>`, for message keys and values.
  - `T` is a `[JsonModel]` type, or an array, list or collection interface of one.
  - Use `SetJakarJsonKeySerializer()` / `SetJakarJsonKeyDeserializer()` for keys.
- **Reading** parses straight from Kafka's buffer, with no copy and no string.
- **`null`:** a `null` value produces a Kafka null (a tombstone), and a null message reads as `default`.
- **Errors:** malformed payloads throw `JsonReadException`, which Kafka reports as a `ConsumeException`.
