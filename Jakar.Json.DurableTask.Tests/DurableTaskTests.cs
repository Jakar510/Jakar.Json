// Jakar.Json.DurableTask.Tests
// 10/17/2026

using Microsoft.DurableTask;
using Microsoft.DurableTask.Converters;



namespace Jakar.Json.DurableTask.Tests;


[TestFixture]
public sealed class DurableTaskTests
{
    private readonly JakarJsonDataConverter __converter = new();


    [Test] public void Models_RoundTrip()
    {
        string json = __converter.Serialize(Samples.Order())!;

        Assert.Multiple(() =>
                        {
                            Assert.That(json,                                                  Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(__converter.Deserialize<Order>(json),                  Is.EqualTo(Samples.Order()));
                            Assert.That(__converter.Deserialize(__converter.Serialize(Samples.Orders()), typeof(List<Order>)), Is.EqualTo(Samples.Orders()));
                        });
    }

    [Test] public void NullAndEmpty()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(__converter.Serialize(null),                   Is.Null.Or.EqualTo("null"), "the fallback's choice");
                            Assert.That(__converter.Deserialize<Order>(null),          Is.Null);
                            Assert.That(__converter.Deserialize<Order>(""),            Is.Null);
                        });
    }

    [Test] public void OtherTypes_UseTheFallback()
    {
        string json = __converter.Serialize(new PlainDto("x"))!;

        Assert.Multiple(() =>
                        {
                            Assert.That(__converter.Fallback,                  Is.SameAs(JsonDataConverter.Default));
                            Assert.That(json,                                  Does.Not.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(__converter.Deserialize<PlainDto>(json), Is.EqualTo(new PlainDto("x")));
                        });
    }

    [Test] public void Corrupt_Throws() => Assert.Throws<JsonReadException>(() => __converter.Deserialize<Order>("{\"id\":"));
}
