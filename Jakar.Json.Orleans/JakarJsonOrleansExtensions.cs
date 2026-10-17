// Jakar.Json.Orleans
// 10/17/2026

using System.Text.Json;
using Jakar.Json.SystemTextJson;
using Orleans.Serialization;



namespace Jakar.Json.Orleans;


public static class JakarJsonOrleansExtensions
{
    /// <summary>
    ///     Serializes grain call arguments, results and state of <c>[JsonModel]</c> types (and arrays, lists and collection interfaces of them) with Jakar.Json, through Orleans' JSON codec;
    ///     every other type keeps Orleans' own serializers: <c>siloBuilder.Services.AddSerializer(s =&gt; s.AddJakarJsonSerializer())</c>.
    /// </summary>
    /// <param name="builder"> Orleans' serializer configuration. </param>
    /// <param name="options"> System.Text.Json options for the codec (defaults when <see langword="null"/>); Jakar.Json's converter is added to them. </param>
    public static ISerializerBuilder AddJakarJsonSerializer( this ISerializerBuilder builder, JsonSerializerOptions? options = null )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddJsonSerializer(isSupported: static type => JsonModels.IsSupported(type), ( options ?? new JsonSerializerOptions() ).AddJakarJson());
    }
}
