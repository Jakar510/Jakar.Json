// Jakar.Json
// 10/02/2026

using Jakar.Json;



namespace Jakar.Extensions;


/// <summary>
///     Source-generates Jakar.Json for a <see langword="partial"/> class, struct, record or record struct: <see cref="IJsonSerializable{TSelf}"/> (reflection-free readers and writers,
///     UTF-16 and UTF-8), <see cref="ISpanFormattable"/> / <see cref="ISpanParsable{TSelf}"/>, and the <c>ToJson</c> / <c>FromJson</c> / <c>TryFromJson</c> helpers.
///     <code>
///     [JsonModel(Naming = JsonNaming.CamelCase)]
///     public sealed partial class Invoice { ... }
///     </code>
///     <para> The settings (<see cref="JsonSettingsAttribute"/>) win over <c>[assembly: JsonDefaults]</c>; <c>[JsonMember]</c> and the member attributes win over both. </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
[Conditional("JAKAR_JSON_KEEP_ATTRIBUTES")]
public sealed class JsonModelAttribute : JsonSettingsAttribute
{
    /// <summary> The member that names the concrete type when <see cref="JsonDerivedAttribute"/>s are present (default <c>"$type"</c>). </summary>
    public string? Discriminator { get; set; }
}
