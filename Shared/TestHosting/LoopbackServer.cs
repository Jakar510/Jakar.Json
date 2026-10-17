// Jakar.Json integration tests (shared source; include it in test projects that reference Microsoft.AspNetCore.App)
// 10/17/2026

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;



namespace Jakar.Json.Tests.Shared;


/// <summary> A real Kestrel server on a free loopback port, for end-to-end tests over HTTP. </summary>
public sealed class LoopbackServer( WebApplication app ) : IAsyncDisposable
{
    public WebApplication App     { get; } = app;
    public Uri            Address { get; } = new(app.Urls.First().TrimEnd('/') + "/");

    public HttpClient CreateClient() => new() { BaseAddress = Address };


    public static async Task<LoopbackServer> StartAsync( Action<WebApplication> map, Action<WebApplicationBuilder>? configure = null )
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        configure?.Invoke(builder);

        WebApplication app = builder.Build();
        map(app);
        await app.StartAsync();
        return new LoopbackServer(app);
    }

    public async ValueTask DisposeAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
    }
}
