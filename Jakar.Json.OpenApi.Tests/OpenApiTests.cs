// Jakar.Json.OpenApi.Tests
// 10/17/2026

using System.Buffers;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;



namespace Jakar.Json.OpenApi.Tests;


[TestFixture]
public sealed class JsonModelContractTests
{
    private static string[] Names<T>() => JsonModelContract.GetMembers(typeof(T))!.Select(static m => m.Name).ToArray();


    // ─── Unit: naming policies and member rules ──────────────────────────────

    [Test] public void NamingPolicies_MatchTheGenerator()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(Names<SnakeModel>(),           Is.EqualTo(new[] { "order_id", "http_status", "ship_to_url2", "unit_price" }));
                            Assert.That(Names<AssemblyDefaultModel>(), Is.EqualTo(new[] { "first-name", "item-count" }), "[assembly: JsonDefaults]");
                            Assert.That(Names<UpperKebabRecord>(),     Is.EqualTo(new[] { "FIRST-VALUE", "SECOND-VALUE" }));
                            Assert.That(Names<UpperSnakeStruct>(),     Is.EqualTo(new[] { "X_VALUE", "IS_ON" }));
                            Assert.That(Names<DeclaredModel>(),        Is.EqualTo(new[] { "camelAlready", "Pascal" }));
                            Assert.That(Names<Order>(),                Is.EqualTo(new[] { "id", "customer", "total", "lines" }));
                            Assert.That(Names<ComputedModel>(),        Is.EqualTo(new[] { "customer_name", "line_count", "label" }), "ReadOnlyMembers = Ignore, with a member opting back in");
                        });
    }

    [Test] public void Attributes_AndVisibility()
    {
        Assert.That(Names<AttributeModel>(), Is.EquivalentTo(new[] { "id", "display_name", "kept", "onlyWhenSet", "internalCount", "publicField", "readOnly" }));
    }

    [Test] public void Inheritance_BaseFirst_OverridesOnce() => Assert.That(Names<DerivedModel>(), Is.EqualTo(new[] { "BaseId", "Label", "Derived" }));

    [Test] public void ReadOnlyMembers_AreWrittenNotRead()
    {
        JsonModelMember readOnly = JsonModelContract.GetMembers(typeof(AttributeModel))!.Single(static m => m.Name == "readOnly");

        Assert.Multiple(() =>
                        {
                            Assert.That(readOnly.IsWritten, Is.True);
                            Assert.That(readOnly.IsRead,    Is.False);
                        });
    }

    [Test] public void NonModels_HaveNoContract()
    {
        Assert.Multiple(() =>
                        {
                            Assert.That(JsonModelContract.GetMembers(typeof(PlainDto)),    Is.Null);
                            Assert.That(JsonModelContract.GetMembers(typeof(List<Order>)), Is.Null);
                            Assert.That(JsonModelContract.IsJsonModel(typeof(string)),     Is.False);
                        });
    }


    // ─── Parity: the contract predicts exactly the keys Jakar.Json writes ────

    [TestCaseSource(typeof(SchemaSamples), nameof(SchemaSamples.All))]
    public void Contract_MatchesWhatJakarJsonWrites( object sample )
    {
        Type                 type  = sample.GetType();
        ArrayBufferWriter<byte> buffer = new();
        Assert.That(JsonCodecRegistry.TryGet(type, out JsonTypeCodec? codec), Is.True, type.Name);
        codec!.Write(sample, ref buffer);

        using JsonDocument document = JsonDocument.Parse(buffer.WrittenMemory);
        string[]           written  = document.RootElement.EnumerateObject().Select(static p => p.Name).ToArray();
        string[]           expected = JsonModelContract.GetMembers(type)!.Where(static m => m.IsWritten).Select(static m => m.Name).ToArray();

        Assert.That(written, Is.EquivalentTo(expected), type.Name);
    }
}



/// <summary> Real OpenAPI documents, served over Kestrel by each generator. </summary>
[TestFixture]
public sealed class OpenApiDocumentTests
{
    private static void MapModels( WebApplication app )
    {
        app.MapGet("/snake",    static () => TypedResults.Ok(new SnakeModel()));
        app.MapGet("/defaults", static () => TypedResults.Ok(new AssemblyDefaultModel()));
        app.MapGet("/attrs",    static () => TypedResults.Ok(new AttributeModel()));
        app.MapGet("/derived",  static () => TypedResults.Ok(new DerivedModel()));
        app.MapGet("/order",    static () => TypedResults.Ok(Samples.Order()));
        app.MapGet("/computed", static () => TypedResults.Ok(new ComputedModel()));
        app.MapGet("/plain",    static () => TypedResults.Ok(new PlainDto("x")));
    }

    private static IEnumerable<TestCaseData> Expectations()
    {
        yield return new TestCaseData("SnakeModel",           new[] { "order_id", "http_status", "ship_to_url2", "unit_price" });
        yield return new TestCaseData("AssemblyDefaultModel", new[] { "first-name", "item-count" });
        yield return new TestCaseData("AttributeModel",       new[] { "id", "display_name", "kept", "onlyWhenSet", "internalCount", "publicField", "readOnly" });
        yield return new TestCaseData("DerivedModel",         new[] { "BaseId", "Label", "Derived" });
        yield return new TestCaseData("Order",                new[] { "id", "customer", "total", "lines" });
        yield return new TestCaseData("ComputedModel",        new[] { "customer_name", "line_count", "label" });
        yield return new TestCaseData("PlainDto",             new[] { "name" }).SetDescription("non-models keep System.Text.Json's (web) names");
    }


    [TestCaseSource(nameof(Expectations))]
    public async Task MicrosoftOpenApi_SchemasUseJakarJsonNames( string schema, string[] expected )
    {
        await using LoopbackServer server = await LoopbackServer.StartAsync(static app =>
                                                                            {
                                                                                MapModels(app);
                                                                                app.MapOpenApi();
                                                                            },
                                                                            static builder => builder.Services.AddOpenApi(static o => o.AddJakarJsonSchemas()));

        Assert.That(await PropertiesAsync(server, "openapi/v1.json", schema), Is.EquivalentTo(expected));
    }

    [TestCaseSource(nameof(Expectations))]
    public async Task Swashbuckle_SchemasUseJakarJsonNames( string schema, string[] expected )
    {
        await using LoopbackServer server = await LoopbackServer.StartAsync(static app =>
                                                                            {
                                                                                MapModels(app);
                                                                                app.UseSwagger();
                                                                            },
                                                                            static builder =>
                                                                            {
                                                                                builder.Services.AddEndpointsApiExplorer();
                                                                                builder.Services.AddSwaggerGen();
                                                                                builder.Services.AddJakarJsonSwagger();
                                                                            });

        Assert.That(await PropertiesAsync(server, "swagger/v1/swagger.json", schema), Is.EquivalentTo(expected));
    }

    [Test] public async Task WithoutTheIntegration_NamesComeFromSystemTextJson()
    {
        // The problem this package solves: System.Text.Json's web naming, not the model's snake_case.
        await using LoopbackServer server = await LoopbackServer.StartAsync(static app =>
                                                                            {
                                                                                MapModels(app);
                                                                                app.MapOpenApi();
                                                                            },
                                                                            static builder => builder.Services.AddOpenApi());

        Assert.That(await PropertiesAsync(server, "openapi/v1.json", "SnakeModel"), Does.Contain("orderId").And.Not.Contain("order_id"));
    }


    private static async Task<string[]> PropertiesAsync( LoopbackServer server, string path, string schema )
    {
        using HttpClient   client   = server.CreateClient();
        using JsonDocument document = JsonDocument.Parse(await client.GetStringAsync(path));

        return document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty(schema).GetProperty("properties").EnumerateObject().Select(static p => p.Name).ToArray();
    }
}
