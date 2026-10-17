// Jakar.Json.AspNetCore.Tests
// 10/17/2026

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;



namespace Jakar.Json.AspNetCore.Tests;


public sealed class TempDataController : Controller
{
    [HttpPost("/tempdata")] public IActionResult Set( [FromBody] TestOrder order )
    {
        TempData["order"] = order;
        TempData["note"]  = "rush";
        return RedirectToAction(nameof(Get));
    }

    [HttpGet("/tempdata")] public IActionResult Get() => Json(new TempDataResult(TempData["order"] as TestOrder, TempData["order"]?.GetType().Name, TempData["note"] as string));
}



public sealed record TempDataResult( TestOrder? Order, string? Type, string? Note );



[TestFixture]
public sealed class TempDataTests
{
    private static readonly TestOrder __order = new() { Id = 7, Customer = "Zoë", Total = 12.50m, Lines = [new TestLine { Sku = "A-1", Quantity = 2, Price = 6.25m }] };


    // ─── The serializer, as MVC resolves it ──────────────────────────────────

    [Test] public void Serializer_DecoratesMvcs_AndRoundTripsModels()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddControllersWithViews().AddJakarJsonTempData().AddJakarJsonTempData(); // idempotent

        using ServiceProvider provider   = services.BuildServiceProvider();
        TempDataSerializer    serializer = provider.GetRequiredService<TempDataSerializer>();

        byte[]                      data = serializer.Serialize(new Dictionary<string, object> { ["order"] = __order, ["note"] = "rush", ["count"] = 3 });
        IDictionary<string, object> back = serializer.Deserialize(data);

        Assert.Multiple(() =>
                        {
                            Assert.That(serializer,                                 Is.InstanceOf<JakarJsonTempDataSerializer>());
                            Assert.That(( (JakarJsonTempDataSerializer)serializer ).Inner, Is.Not.InstanceOf<JakarJsonTempDataSerializer>());
                            Assert.That(serializer.CanSerializeType(typeof(TestOrder)), Is.True, "MVC's default refuses complex types");
                            Assert.That(back["order"],                              Is.EqualTo(__order));
                            Assert.That(back["note"],                               Is.EqualTo("rush"));
                            Assert.That(back["count"],                              Is.EqualTo(3));
                        });
    }

    [Test] public void WithoutViews_Throws() => Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddControllers().AddJakarJsonTempData());


    // ─── End to end: the cookie TempData provider over Kestrel ───────────────

    [Test] public async Task TempData_CarriesAModelAcrossARedirect()
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(TempDataController).Assembly).AddJakarJsonTempData();

        await using WebApplication app = builder.Build();
        app.MapControllers();
        await app.StartAsync();

        using HttpClient client = new(new HttpClientHandler { CookieContainer = new CookieContainer() }) { BaseAddress = new Uri(app.Urls.First()) };

        using HttpResponseMessage response = await client.PostAsJsonAsync("/tempdata", __order); // follows the redirect, sending the TempData cookie
        TempDataResult?           result   = await response.Content.ReadFromJsonAsync<TempDataResult>();

        Assert.Multiple(() =>
                        {
                            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                            Assert.That(result!.Order,       Is.EqualTo(__order));
                            Assert.That(result.Type,         Is.EqualTo(nameof(TestOrder)));
                            Assert.That(result.Note,         Is.EqualTo("rush"));
                        });

        await app.StopAsync();
    }
}
