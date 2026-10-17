// Jakar.Json.Kafka.Tests
// 10/17/2026

using System.Text;
using Confluent.Kafka;



namespace Jakar.Json.Kafka.Tests;


[TestFixture]
public sealed class KafkaTests
{
    private static readonly SerializationContext __value = new(MessageComponentType.Value, "orders");
    private static readonly SerializationContext __key   = new(MessageComponentType.Key,   "orders");


    // ─── Unit ────────────────────────────────────────────────────────────────

    [Test] public async Task Values_RoundTrip_SyncAndAsync()
    {
        JakarJsonKafkaSerializer<Order> serializer = new();
        byte[]                          bytes      = serializer.Serialize(Samples.Order(), __value);

        Assert.Multiple(async () =>
                        {
                            Assert.That(Encoding.UTF8.GetString(bytes),                                              Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(serializer.Deserialize(bytes, false, __value),                               Is.EqualTo(Samples.Order()));
                            Assert.That(await serializer.DeserializeAsync(await serializer.SerializeAsync(Samples.Order(), __value), false, __value), Is.EqualTo(Samples.Order()));
                        });
        await Task.CompletedTask;
    }

    [Test] public void CollectionsStructsAndKeys_RoundTrip()
    {
        JakarJsonKafkaSerializer<List<Order>> list  = new();
        JakarJsonKafkaSerializer<Point>       point = new();

        Assert.Multiple(() =>
                        {
                            Assert.That(list.Deserialize(list.Serialize(Samples.Orders(), __value), false, __value),                         Is.EqualTo(Samples.Orders()));
                            Assert.That(point.Deserialize(point.Serialize(new Point { X = 1, Y = 2 }, __key), false, __key),                 Is.EqualTo(new Point { X = 1, Y = 2 }));
                        });
    }

    [Test] public void Null_IsKafkaNull()
    {
        JakarJsonKafkaSerializer<Order?> serializer = new();

        Assert.Multiple(() =>
                        {
                            Assert.That(serializer.Serialize(null, __value),                   Is.Null, "a tombstone");
                            Assert.That(serializer.Deserialize([], isNull: true, __value),     Is.Null);
                        });
    }

    [Test] public void Corrupt_Throws() => Assert.Throws<JsonReadException>(() => new JakarJsonKafkaSerializer<Order>().Deserialize("{\"id\":"u8, false, __value));

    [Test] public void Unsupported_Throws() => Assert.Throws<NotSupportedException>(() => _ = new JakarJsonKafkaSerializer<PlainDto>());


    // ─── Integration: a real producer against librdkafka's in-process mock cluster ───

    [Test] public async Task Producer_SerializesThroughTheClientPipeline()
    {
        ProducerConfig config = new() { BootstrapServers = "localhost:9092", MessageTimeoutMs = 10_000 };
        config.Set("test.mock.num.brokers", "1"); // librdkafka starts an in-process mock cluster and ignores BootstrapServers

        using IProducer<Point, Order> producer = new ProducerBuilder<Point, Order>(config).SetJakarJsonKeySerializer().SetJakarJsonValueSerializer().Build();

        DeliveryResult<Point, Order> result = await producer.ProduceAsync("orders", new Message<Point, Order> { Key = new Point { X = 7, Y = 0 }, Value = Samples.Order() });

        Assert.Multiple(() =>
                        {
                            Assert.That(result.Status,  Is.EqualTo(PersistenceStatus.Persisted));
                            Assert.That(result.Message.Value, Is.EqualTo(Samples.Order()));
                        });
    }

    [Test] public void Consumer_BuildsWithTheDeserializers()
    {
        ConsumerConfig config = new() { BootstrapServers = "localhost:9092", GroupId = "tests" };
        config.Set("test.mock.num.brokers", "1");

        using IConsumer<Point, Order> consumer = new ConsumerBuilder<Point, Order>(config).SetJakarJsonKeyDeserializer().SetJakarJsonValueDeserializer().Build();
        Assert.That(consumer.Name, Is.Not.Empty);
    }
}
