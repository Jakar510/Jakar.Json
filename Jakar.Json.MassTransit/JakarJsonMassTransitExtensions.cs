// Jakar.Json.MassTransit
// 10/17/2026

using Jakar.Json.SystemTextJson;
using MassTransit;



namespace Jakar.Json.MassTransit;


public static class JakarJsonMassTransitExtensions
{
    /// <summary>
    ///     Reads and writes messages of <c>[JsonModel]</c> types (and arrays, lists and collection interfaces of them) with Jakar.Json, inside MassTransit's JSON envelope. Headers, the
    ///     envelope and every other type keep System.Text.Json.
    /// </summary>
    /// <remarks> MassTransit's System.Text.Json options are process-wide, so this applies to every bus in the process. </remarks>
    /// <param name="configurator"> The bus being configured (<c>x.UsingRabbitMq((context, cfg) =&gt; cfg.UseJakarJson())</c>). </param>
    /// <param name="configure"> Adds types for Native AOT: <c>f =&gt; f.Add&lt;OrderSubmitted&gt;()</c>. </param>
    public static void UseJakarJson( this IBusFactoryConfigurator configurator, Action<JakarJsonConverterFactory>? configure = null )
    {
        ArgumentNullException.ThrowIfNull(configurator);
        configurator.ConfigureJsonSerializerOptions(options => options.AddJakarJson(configure));
    }
}
