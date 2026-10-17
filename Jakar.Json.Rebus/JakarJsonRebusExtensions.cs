// Jakar.Json.Rebus
// 10/17/2026

using System.Text;
using System.Text.Json;
using Jakar.Json.SystemTextJson;
using Rebus.Config;
using Rebus.Serialization;
using Rebus.Serialization.Json;



namespace Jakar.Json.Rebus;


public static class JakarJsonRebusExtensions
{
    /// <summary>
    ///     Uses Rebus' System.Text.Json serializer with <c>[JsonModel]</c> message bodies (and arrays, lists and collection interfaces of them) written and read by Jakar.Json. Rebus' headers and
    ///     every other type keep System.Text.Json: <c>.Serialization(s =&gt; s.UseJakarJson())</c>.
    /// </summary>
    /// <param name="configurer"> Rebus' serializer configuration. </param>
    /// <param name="options"> Options for the other types (Rebus' defaults when <see langword="null"/>); Jakar.Json's converter is added to them. </param>
    public static void UseJakarJson( this StandardConfigurer<ISerializer> configurer, JsonSerializerOptions? options = null )
    {
        ArgumentNullException.ThrowIfNull(configurer);
        configurer.UseSystemTextJson(( options ?? new JsonSerializerOptions() ).AddJakarJson(), Encoding.UTF8);
    }
}
