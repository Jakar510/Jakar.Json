// Jakar.Json.Tests
// 10/07/2026

namespace Jakar.Json.Tests;


// ─── Settings ────────────────────────────────────────────────────────────────



[JsonModel(Naming = JsonNaming.SnakeCaseLower, NullValues = JsonNullValues.Omit, DefaultValues = JsonDefaultValues.Omit)]
public sealed partial class SnakeOmit
{
    public int     HTTPStatusCode { get; set; }
    public string? DisplayName    { get; set; }
    public double  Score          { get; set; }
}



[JsonModel(NameMatching = JsonNameMatching.OrdinalIgnoreCase, DuplicateMembers = JsonDuplicateMembers.LastWins, NumbersFromStrings = JsonNumbersFromStrings.Allow, AllowComments = JsonToggle.On, AllowTrailingCommas = JsonToggle.On)]
public sealed partial class Lenient
{
    public int    Count { get; set; }
    public double Ratio { get; set; }
    public string Name  { get; set; } = "";
}



[JsonModel(LargeIntegers = JsonLargeIntegers.String, NonFiniteFloats = JsonNonFiniteFloats.AsString, Enums = JsonEnumFormat.Number)]
public sealed partial class Numeric
{
    public long   Big    { get; set; }
    public long   Small  { get; set; }
    public double Weird  { get; set; }
    public Status Status { get; set; }
}



[JsonModel(Indented = JsonToggle.On, IndentChar = JsonIndentChar.Space, IndentSize = 4, GenerateToString = JsonToggle.Off)]
public sealed partial class Pretty
{
    public int[] Values { get; set; } = [];

    public override string ToString() => "custom";
}



// ─── Polymorphism ────────────────────────────────────────────────────────────



[JsonModel(Discriminator = "kind")]
[JsonDerived(typeof(Dog),   "dog")]
[JsonDerived(typeof(Cat),   "cat")]
[JsonDerived(typeof(Puppy), "puppy")]
public abstract partial class Animal
{
    public string Name { get; set; } = "";
}



[JsonModel]
public partial class Dog : Animal
{
    public bool GoodBoy { get; set; }
}



[JsonModel]
public sealed partial class Puppy : Dog
{
    public int Weeks { get; set; }
}



[JsonModel]
public sealed partial class Cat : Animal
{
    public int Lives { get; set; } = 9;
}



[JsonModel]
public sealed partial class Zoo
{
    public List<Animal> Animals { get; set; } = [];
    public Animal?      Star    { get; set; }
}



// ─── Extension data, converters, nodes ───────────────────────────────────────



[JsonModel]
public sealed partial class Bag
{
    public string Title { get; set; } = "";

    [JsonMember(ExtensionData = true)] public JObjectNode? Extra { get; set; }
}



/// <summary> Unix seconds instead of ISO 8601. </summary>
public readonly struct UnixSecondsConverter : IJsonConverter<DateTimeOffset>
{
    public static void Write<TWriter>( ref TWriter writer, scoped in DateTimeOffset value )
        where TWriter : IJsonWriter, allows ref struct => writer.WriteInteger(value.ToUnixTimeSeconds());

    public static bool TryRead<TReader>( ref TReader reader, out DateTimeOffset value )
        where TReader : IJsonReader, allows ref struct
    {
        value = default;
        if ( !reader.TryReadInteger(out long seconds) ) { return false; }

        value = DateTimeOffset.FromUnixTimeSeconds(seconds);
        return true;
    }
}



[JsonModel]
public sealed partial class Converted
{
    [JsonMember(Converter = typeof(UnixSecondsConverter))] public DateTimeOffset  When  { get; set; }
    [JsonMember(Converter = typeof(UnixSecondsConverter))] public DateTimeOffset? Maybe { get; set; }
    public                                                        JNode?          Any   { get; set; }
    public                                                        JArrayNode      Array { get; set; } = [];
}



// ─── Structs, generics, fields, constructors ─────────────────────────────────



[JsonModel]
public partial struct Point
{
    public          int X;
    public          int Y;
    public readonly int Sum => X + Y;
}



[JsonModel] public readonly partial record struct Money( decimal Amount, string Currency = "USD" );



[JsonModel]
public sealed partial class Page<T>
    where T : IJsonSerializable<T>
{
    public List<T> Items { get; set; } = [];
    public int     Total { get; set; }
}



[JsonModel]
public sealed partial class Keyed<TKey>
    where TKey : ISpanFormattable, ISpanParsable<TKey>
{
    public TKey?                   Single { get; set; }
    public Dictionary<string, int> Counts { get; set; } = [];
}



[JsonModel]
public sealed partial class Account
{
    [JsonCreator] public Account( string login, int age )
    {
        Login = login;
        Age   = age;
    }

    public string Login { get; }
    public int    Age   { get; }

    [JsonMember(Required = true)] public string Email { get; set; } = "";
}



// ─── Member attributes ───────────────────────────────────────────────────────



[JsonModel]
public sealed partial class Shorthand
{
    [JsonKey("full_name")] public string Name { get; set; } = "";

    [JsonOrder(-1)] public int First { get; set; }

    [JsonSkip] public string Secret { get; set; } = "hidden";

    [JsonSkip(Condition = JsonSkipCondition.WhenNull)] public string? Optional { get; set; }

    [JsonSkip(Condition = JsonSkipCondition.WhenDefault)] public int Zero { get; set; }

    [JsonOptIn] internal int Internal { get; set; }

    [JsonMandatory] public string Code { get; set; } = "";

    [JsonMember(Name = "wins")] [JsonKey("loses")] public int Both { get; set; }

    [JsonMember(Ignore = false)] [JsonSkip] public int Kept { get; set; }
}



// ─── Read-only (computed) members ────────────────────────────────────────────



[JsonModel]
public sealed partial class Computed
{
    public int A { get; set; }
    public int B { get; set; }

    public int Sum => A + B; // written, never read (JJSON011)

    [JsonMember(ReadOnly = JsonReadOnlyMembers.Ignore)] public int Product => A * B;
}



[JsonModel(ReadOnlyMembers = JsonReadOnlyMembers.Ignore)]
public sealed partial class ComputedIgnored
{
    public int A { get; set; }

    public int Twice => A * 2;

    [JsonMember(ReadOnly = JsonReadOnlyMembers.Write)] public int Thrice => A * 3;

    [JsonSkip] public int Hidden { get; set; } = 5;
}



/// <summary> A get-only member bound to a constructor parameter is read through it, so <c>ReadOnlyMembers = Ignore</c> keeps it. </summary>
[JsonModel(ReadOnlyMembers = JsonReadOnlyMembers.Ignore)]
public sealed partial class ComputedBound( int x )
{
    public int X      { get; } = x;
    public int Double => X * 2;
}



// ─── Extension data ([JsonOverflow]) ────────────────────────────────────────



[JsonModel]
public sealed partial class Overflowing
{
    public string Name { get; set; } = "";

    [JsonOverflow] public OrderedDictionary<string, JNode?>? Extra { get; set; }
}



/// <summary> A get-only bag: captured members are added to the instance the type created. </summary>
[JsonModel(Naming = JsonNaming.CamelCase)]
public sealed partial class OverflowingNode
{
    public int Id { get; set; }

    [JsonOverflow] public JObjectNode Rest { get; } = new();
}



[JsonModel]
public sealed partial record OverflowingRecord( string Name )
{
    [JsonOverflow] public JObjectNode? Rest { get; init; }
}



// ─── Non-finite floats ───────────────────────────────────────────────────────



[JsonModel]
public sealed partial class Floats
{
    public double                     Double   { get; set; }
    public float                      Single   { get; set; }
    public Half                       Half     { get; set; }
    public double?                    Nullable { get; set; }
    public double[]                   Array    { get; set; } = [];
    public Dictionary<string, double> Map      { get; set; } = [];
    public decimal                    Decimal  { get; set; }
}



[JsonModel(NonFiniteFloats = JsonNonFiniteFloats.Error)]
public sealed partial class StrictFloats
{
    public double Value { get; set; }
}



// ─── Nested types ────────────────────────────────────────────────────────────



public static partial class Outer
{
    [JsonModel(Naming = JsonNaming.KebabCaseLower)] public sealed partial record Inner( string FirstValue, int? SecondValue );
}
