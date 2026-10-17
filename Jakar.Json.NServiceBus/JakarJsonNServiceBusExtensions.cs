// Jakar.Json.NServiceBus
// 10/17/2026

using System.Text.Json;
using Jakar.Json.SystemTextJson;
using NServiceBus;
using NServiceBus.Serialization;



namespace Jakar.Json.NServiceBus;


public static class JakarJsonNServiceBusExtensions
{
    /// <summary>
    ///     Serializes messages with NServiceBus' System.Text.Json serializer, with <c>[JsonModel]</c> types (and arrays, lists and collection interfaces of them) written and read by Jakar.Json.
    ///     Headers and every other type keep System.Text.Json, so endpoints without Jakar.Json still read the messages.
    /// </summary>
    /// <param name="configuration"> The endpoint. </param>
    /// <param name="options"> Options for the other types (System.Text.Json's defaults when <see langword="null"/>); Jakar.Json's converter is added to them. </param>
    public static SerializationExtensions<SystemJsonSerializer> UseJakarJsonSerialization( this EndpointConfiguration configuration, JsonSerializerOptions? options = null )
    {
        ArgumentNullException.ThrowIfNull(configuration);

        SerializationExtensions<SystemJsonSerializer> serialization = configuration.UseSerialization<SystemJsonSerializer>();
        serialization.Options(( options ?? new JsonSerializerOptions() ).AddJakarJson());
        return serialization;
    }
}
