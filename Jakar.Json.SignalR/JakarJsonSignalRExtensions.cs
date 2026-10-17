// Jakar.Json.SignalR
// 10/17/2026

using Jakar.Json.SystemTextJson;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;



namespace Jakar.Json.SignalR;


public static class JakarJsonSignalRExtensions
{
    /// <summary>
    ///     Uses SignalR's JSON hub protocol with <c>[JsonModel]</c> arguments and results (and arrays, lists and collection interfaces of them) written and read by Jakar.Json. Works on the
    ///     server (<c>services.AddSignalR().AddJakarJsonProtocol()</c>) and on .NET clients (<c>new HubConnectionBuilder().AddJakarJsonProtocol()</c>). The protocol is still "json", so
    ///     clients and servers without Jakar.Json (JavaScript, other .NET apps) interoperate.
    /// </summary>
    /// <param name="builder"> The SignalR server or client builder. </param>
    /// <param name="configure"> Further JSON options, and the types to add for Native AOT (<c>o =&gt; o.PayloadSerializerOptions.AddJakarJson(f =&gt; f.Add&lt;Order&gt;())</c>). </param>
    public static TBuilder AddJakarJsonProtocol<TBuilder>( this TBuilder builder, Action<JsonHubProtocolOptions>? configure = null )
        where TBuilder : ISignalRBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddJsonProtocol(options =>
                                       {
                                           options.PayloadSerializerOptions.AddJakarJson();
                                           configure?.Invoke(options);
                                       });
    }
}
