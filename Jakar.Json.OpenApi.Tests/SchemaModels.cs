// Jakar.Json.OpenApi.Tests
// 10/17/2026

using Jakar.Extensions;
using Jakar.Json;

// Models without their own naming in this assembly are kebab-case (the shared test models set camelCase themselves).
[assembly: JsonDefaults(Naming = JsonNaming.KebabCaseLower)]



namespace Jakar.Json.OpenApi.Tests;


[JsonModel(Naming = JsonNaming.SnakeCaseLower)]
public sealed partial class SnakeModel
{
    public int    OrderId      { get; set; }
    public string HTTPStatus   { get; set; } = "";
    public string ShipToURL2   { get; set; } = "";
    public double UnitPrice    { get; set; }
}



/// <summary> Takes the assembly's kebab-case default. </summary>
[JsonModel]
public sealed partial class AssemblyDefaultModel
{
    public string FirstName { get; set; } = "";
    public int    ItemCount { get; set; }
}



[JsonModel(Naming = JsonNaming.CamelCase)]
public sealed partial class AttributeModel
{
    [JsonMember(Name = "id")]                public int    Identifier   { get; set; }
    [JsonKey("display_name")]                public string Name         { get; set; } = "";
    [JsonMember(Ignore = true)]              public string Secret       { get; set; } = "";
    [JsonSkip]                               public string Skipped      { get; set; } = "";
    [JsonSkip, JsonMember(Name = "kept", Ignore = false)] public string Overridden { get; set; } = "";
    [JsonSkip(Condition = JsonSkipCondition.WhenNull)] public string? OnlyWhenSet { get; set; }
    [JsonMember]                             internal int  InternalCount { get; set; }
    internal                                  int          NotIncluded  { get; set; }
    public                                    int          PublicField;
    public string                                          ReadOnly     => Name + "!";
}



[JsonModel(Naming = JsonNaming.PascalCase)]
public partial class BaseModel
{
    public         int    baseId   { get; set; }
    public virtual string label    { get; set; } = "";
}



[JsonModel(Naming = JsonNaming.PascalCase)]
public sealed partial class DerivedModel : BaseModel
{
    public override string label   { get; set; } = "";
    public          string derived { get; set; } = "";
}



[JsonModel(Naming = JsonNaming.KebabCaseUpper)]
public sealed partial record UpperKebabRecord( string FirstValue, int SecondValue );



[JsonModel(Naming = JsonNaming.SnakeCaseUpper)]
public partial struct UpperSnakeStruct
{
    public int  XValue { get; set; }
    public bool IsOn   { get; set; }
}



[JsonModel(Naming = JsonNaming.AsDeclared)]
public sealed partial class DeclaredModel
{
    public string camelAlready { get; set; } = "";
    public string Pascal       { get; set; } = "";
}



/// <summary> Computed members are left out (<c>ReadOnlyMembers = Ignore</c>), except the one that opts back in. </summary>
[JsonModel(Naming = JsonNaming.SnakeCaseLower, ReadOnlyMembers = JsonReadOnlyMembers.Ignore)]
public sealed partial class ComputedModel
{
    public string CustomerName { get; set; } = "";
    public int    LineCount    { get; set; }
    public int    Doubled      => LineCount * 2;

    [JsonMember(ReadOnly = JsonReadOnlyMembers.Write)] public string Label => CustomerName + "!";
}



public static class SchemaSamples
{
    public static IEnumerable<object> All() =>
    [
        new SnakeModel { OrderId = 1, HTTPStatus = "ok", ShipToURL2 = "u", UnitPrice = 2.5 },
        new AssemblyDefaultModel { FirstName = "a", ItemCount = 2 },
        new AttributeModel { Identifier = 1, Name = "n", Secret = "s", Skipped = "i", Overridden = "o", OnlyWhenSet = "w", InternalCount = 3, NotIncluded = 4, PublicField = 5 },
        new DerivedModel { baseId = 1, label = "l", derived = "d" },
        new UpperKebabRecord("f", 2),
        new UpperSnakeStruct { XValue = 1, IsOn = true },
        new DeclaredModel { camelAlready = "c", Pascal = "p" },
        new ComputedModel { CustomerName = "c", LineCount = 1 },
        Samples.Order()
    ];
}
