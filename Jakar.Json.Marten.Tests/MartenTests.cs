// Jakar.Json.Marten.Tests
// 10/17/2026

using System.Text;
using Marten;
using ISerializer = Marten.ISerializer;



namespace Jakar.Json.Marten.Tests;


/// <summary> Marten's configured serializer (no database needed): what it writes to and reads from jsonb columns. </summary>
[TestFixture]
public sealed class MartenTests
{
    private static ISerializer Serializer()
    {
        StoreOptions options = new();
        options.UseJakarJson();
        return options.Serializer();
    }


    [Test] public void Documents_AreJakarJson_AndRoundTrip()
    {
        ISerializer serializer = Serializer();
        string      json       = serializer.ToJson(Samples.Order());

        Assert.Multiple(() =>
                        {
                            Assert.That(json,                                                                  Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(serializer.FromJson<Order>(new MemoryStream(Encoding.UTF8.GetBytes(json))), Is.EqualTo(Samples.Order()));
                        });
    }

    [Test] public async Task Async_AndCollections()
    {
        ISerializer serializer = Serializer();
        string      json       = serializer.ToJson(Samples.Orders());

        Assert.That(await serializer.FromJsonAsync<List<Order>>(new MemoryStream(Encoding.UTF8.GetBytes(json))), Is.EqualTo(Samples.Orders()));
    }

    [Test] public void Casing_IsCamelCaseByDefault()
    {
        StoreOptions options = new();
        options.UseJakarJson();
        Assert.That(options.Serializer().ToJson(new PlainDto("x")), Does.Contain("\"name\""), "other types use the same casing Marten's LINQ assumes");
    }
}
