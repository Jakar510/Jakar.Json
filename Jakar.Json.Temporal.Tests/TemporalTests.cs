// Jakar.Json.Temporal.Tests
// 10/17/2026

using System.Text;
using Temporalio.Api.Common.V1;
using Temporalio.Converters;



namespace Jakar.Json.Temporal.Tests;


[TestFixture]
public sealed class TemporalTests
{
    private static readonly DataConverter __converter = DataConverter.Default.WithJakarJson();


    [Test] public void PayloadConverter_ReplacesJsonPlain_KeepsTheOtherEncodings()
    {
        DefaultPayloadConverter converter = JakarJsonTemporalExtensions.CreatePayloadConverter();
        string[]                defaults  = new DefaultPayloadConverter().EncodingConverters.Select(static c => c.Encoding).ToArray();

        Assert.Multiple(() =>
                        {
                            Assert.That(converter.EncodingConverters.Select(static c => c.Encoding), Is.EqualTo(defaults), "same encodings, same order");
                            Assert.That(converter.EncodingConverters.OfType<JakarJsonPlainConverter>().Count(), Is.EqualTo(1));
                            Assert.That(converter.EncodingConverters.OfType<JsonPlainConverter>(),            Is.Empty);
                        });
    }

    [Test] public void Models_AreJsonPlainPayloads_WrittenByJakarJson()
    {
        Payload payload = __converter.PayloadConverter.ToPayload(Samples.Order());

        Assert.Multiple(() =>
                        {
                            Assert.That(payload.Metadata["encoding"].ToStringUtf8(),                        Is.EqualTo("json/plain"));
                            Assert.That(payload.Data.ToStringUtf8(),                                        Does.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(__converter.PayloadConverter.ToValue(payload, typeof(Order)),       Is.EqualTo(Samples.Order()));
                        });
    }

    [Test] public void OtherValues_KeepTemporalsConverters()
    {
        Payload text  = __converter.PayloadConverter.ToPayload(new PlainDto("x"));
        Payload empty = __converter.PayloadConverter.ToPayload(null);

        Assert.Multiple(() =>
                        {
                            Assert.That(text.Data.ToStringUtf8(),                                           Does.Not.Contain(Samples.JAKAR_SEPARATOR));
                            Assert.That(__converter.PayloadConverter.ToValue(text, typeof(PlainDto)),       Is.EqualTo(new PlainDto("x")));
                            Assert.That(empty.Metadata["encoding"].ToStringUtf8(),                          Is.EqualTo("binary/null"));
                        });
    }

    [Test] public void Collections_RoundTrip()
    {
        Payload payload = __converter.PayloadConverter.ToPayload(Samples.Orders());
        Assert.That(__converter.PayloadConverter.ToValue(payload, typeof(List<Order>)), Is.EqualTo(Samples.Orders()));
    }

    [Test] public void WithJakarJson_KeepsTheFailureConverter() => Assert.That(__converter.FailureConverter, Is.SameAs(DataConverter.Default.FailureConverter));
}
