// Jakar.Json
// 10/23/2026

namespace Jakar.Json;


// Single-purpose member attributes: each is a shorthand for one [JsonMember] setting (SPEC.md §4.4). When both are present, [JsonMember]'s explicit values win.



/// <summary> The JSON name, used verbatim (the naming policy doesn't apply). Same as <c>[JsonMember(Name = ...)]</c>. </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
[Conditional("JAKAR_JSON_KEEP_ATTRIBUTES")]
public sealed class JsonKeyAttribute( string name ) : Attribute
{
    public string Name { get; } = name;
}



/// <summary> Members are written by order (default 0), then by declaration. Same as <c>[JsonMember(Order = ...)]</c>. </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
[Conditional("JAKAR_JSON_KEEP_ATTRIBUTES")]
public sealed class JsonOrderAttribute( int order ) : Attribute
{
    public int Order { get; } = order;
}



/// <summary> When <see cref="JsonSkipAttribute"/> leaves a member out. </summary>
public enum JsonSkipCondition : byte
{
    /// <summary> Never read or written (<c>[JsonMember(Ignore = true)]</c>). </summary>
    Always,

    /// <summary> Not written when <see langword="null"/> (<c>NullValues = Omit</c>). </summary>
    WhenNull,

    /// <summary> Not written when it's the type's default (<c>DefaultValues = Omit</c>). </summary>
    WhenDefault
}



/// <summary> Leaves the member out of the JSON: always (the default), or only when writing <see langword="null"/> or the default value. </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
[Conditional("JAKAR_JSON_KEEP_ATTRIBUTES")]
public sealed class JsonSkipAttribute : Attribute
{
    public JsonSkipCondition Condition { get; set; } = JsonSkipCondition.Always;
}



/// <summary> Reading fails when the member is missing. Same as <c>[JsonMember(Required = true)]</c>. </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
[Conditional("JAKAR_JSON_KEEP_ATTRIBUTES")]
public sealed class JsonMandatoryAttribute : Attribute;



/// <summary> Serializes a non-public member (or a field), which <c>[JsonModel]</c> otherwise leaves out. Private members of base types can't be reached. </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
[Conditional("JAKAR_JSON_KEEP_ATTRIBUTES")]
public sealed class JsonOptInAttribute : Attribute;



/// <summary>
///     Collects the JSON members the type doesn't declare, in input order, and writes them back after the declared members: an <c>OrderedDictionary&lt;string, JNode?&gt;</c> or a
///     <see cref="JObjectNode"/>. Implies <c>UnknownMembers = Capture</c>. Same as <c>[JsonMember(ExtensionData = true)]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true)]
[Conditional("JAKAR_JSON_KEEP_ATTRIBUTES")]
public sealed class JsonOverflowAttribute : Attribute;



/// <summary> The constructor <c>[JsonModel]</c> reads through, when the type has several. Its parameters bind to members by name (ignoring case). </summary>
[AttributeUsage(AttributeTargets.Constructor)]
[Conditional("JAKAR_JSON_KEEP_ATTRIBUTES")]
public sealed class JsonCreatorAttribute : Attribute;
