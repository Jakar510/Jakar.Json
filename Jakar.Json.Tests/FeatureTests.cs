// Jakar.Json.Tests
// 10/07/2026

namespace Jakar.Json.Tests;


[TestFixture]
public sealed class FeatureTests
{
    // ─── Settings ────────────────────────────────────────────────────────────

    [Test] public void SnakeCase_AndOmittedNullsAndDefaults()
    {
        Assert.That(new SnakeOmit
                    {
                        HTTPStatusCode = 404,
                        DisplayName    = "x",
                        Score          = 1.5
                    }.ToJson(),
                    Is.EqualTo("""{"http_status_code" : 404,"display_name" : "x","score" : 1.5}"""));

        Assert.That(new SnakeOmit().ToJson(), Is.EqualTo("{}"));
    }

    [Test] public void Lenient_IgnoresCase_LastWins_NumbersFromStrings_Comments_TrailingCommas()
    {
        const string JSON = """
                            {
                                // a comment
                                "COUNT": "5",
                                "ratio": "0.5",
                                "name": "first",
                                "Name": "second", /* last wins */
                            }
                            """;

        Lenient value = Lenient.FromJson(JSON);
        Assert.That(value.Count,                                         Is.EqualTo(5));
        Assert.That(value.Ratio,                                         Is.EqualTo(0.5));
        Assert.That(value.Name,                                          Is.EqualTo("second"));
        Assert.That(Lenient.FromJson(Encoding.UTF8.GetBytes(JSON)).Name, Is.EqualTo("second"), "UTF-8 ignore-case matching");
    }

    [Test] public void LargeIntegers_NonFinite_EnumNumbers()
    {
        Numeric value = new()
                        {
                            Big    = long.MaxValue,
                            Small  = 42,
                            Weird  = double.NegativeInfinity,
                            Status = Status.Paid
                        };

        string json = value.ToJson();

        Assert.That(json, Is.EqualTo("""{"Big" : "9223372036854775807","Small" : 42,"Weird" : "-Infinity","Status" : 2}"""));

        Numeric back = Numeric.FromJson(json);
        Assert.That(back.Big,                                            Is.EqualTo(long.MaxValue));
        Assert.That(back.Weird,                                          Is.EqualTo(double.NegativeInfinity));
        Assert.That(back.Status,                                         Is.EqualTo(Status.Paid));
        Assert.That(Numeric.FromJson("""{"Weird":"NaN"}""").Weird,       Is.NaN);
        Assert.That(Numeric.FromJson("""{"Status":"Paid"}""").Status,    Is.EqualTo(Status.Paid), "Enums = Number still reads names (what a name-writing serializer produces)");
        Assert.That(Numeric.TryFromJson("""{"Status":"Nope"}""", out _), Is.False);
    }

    [Test] public void NonFiniteFloats_RoundTripByDefault()
    {
        Floats value = new()
                       {
                           Double   = double.NaN,
                           Single   = float.PositiveInfinity,
                           Half     = Half.NegativeInfinity,
                           Nullable = double.NaN,
                           Array    = [1.5, double.NaN, double.NegativeInfinity],
                           Map      = new Dictionary<string, double> { ["x"] = double.PositiveInfinity }
                       };

        string json = value.ToJson();

        Assert.That(json, Is.EqualTo("""{"Double" : "NaN","Single" : "Infinity","Half" : "-Infinity","Nullable" : "NaN","Array" : [1.5,"NaN","-Infinity"],"Map" : {"x" : "Infinity"},"Decimal" : 0}"""));

        Floats back = Floats.FromJson(json);
        Assert.That(back.Double,   Is.NaN);
        Assert.That(back.Single,   Is.EqualTo(float.PositiveInfinity));
        Assert.That(back.Half,     Is.EqualTo(Half.NegativeInfinity));
        Assert.That(back.Nullable, Is.NaN);
        Assert.That(back.Array,    Is.EqualTo(new[] { 1.5, double.NaN, double.NegativeInfinity }));
        Assert.That(back.Map["x"], Is.EqualTo(double.PositiveInfinity));
        Assert.That(Floats.FromJson(Encoding.UTF8.GetBytes(json)).Double, Is.NaN, "UTF-8");

        Assert.That(Floats.TryFromJson("""{"Decimal":"NaN"}""", out _), Is.False, "decimal has no NaN");
        Assert.That(Floats.TryFromJson("""{"Double":NaN}""",    out _), Is.False, "JSON has no bare NaN literal");
        Assert.That(Floats.TryFromJson("""{"Double":"nan"}""",  out _), Is.False, "the names are case-sensitive");
        Assert.That(new Scalars { Double = double.NaN }.ToJson(),        Does.Contain("\"NaN\""));
    }

    [Test] public void NonFiniteFloats_Error_IsOptIn()
    {
        Assert.Throws<JsonWriteException>(() => new StrictFloats { Value = double.NaN }.ToJson());
        Assert.That(StrictFloats.TryFromJson("""{"Value":"NaN"}""", out _), Is.False);
        Assert.That(StrictFloats.FromJson("""{"Value":1.25}""").Value,       Is.EqualTo(1.25));
    }

    [Test] public void ReadOnlyMembers_WriteByDefault_IgnoreOnRequest()
    {
        Computed computed = new()
                            {
                                A = 2,
                                B = 3
                            };

        Assert.That(computed.ToJson(),                                     Is.EqualTo("""{"A" : 2,"B" : 3,"Sum" : 5}"""), "Sum is written; Product is [JsonMember(ReadOnly = Ignore)]");
        Assert.That(Computed.FromJson("""{"A":1,"B":1,"Sum":99}""").Sum, Is.EqualTo(2),                                 "a computed member in the input is skipped");

        ComputedIgnored ignored = new() { A = 2 };
        Assert.That(ignored.ToJson(),                                         Is.EqualTo("""{"A" : 2,"Thrice" : 6}"""), "Twice is ignored by the type; Thrice opts back in");
        Assert.That(ComputedIgnored.FromJson("""{"A":4,"Twice":1}""").Twice, Is.EqualTo(8));

        Assert.That(new ComputedBound(4).ToJson(),               Is.EqualTo("""{"X" : 4}"""), "X is bound to the constructor, so it's kept");
        Assert.That(ComputedBound.FromJson("""{"X":5}""").Double, Is.EqualTo(10));
    }

    [Test] public void Overflow_CapturesAndWritesUnknownMembers()
    {
        const string JSON = """{"Name" : "x","extra" : {"n" : [1,2.50,1e400,"s",null,true]},"flag" : false}""";

        Overflowing value = Overflowing.FromJson(JSON);
        Assert.That(value.Name,                                   Is.EqualTo("x"));
        Assert.That(value.Extra!.Keys,                            Is.EqualTo(new[] { "extra", "flag" }), "input order");
        Assert.That(value.Extra["extra"]!["n"]![1]!.ToJson(),     Is.EqualTo("2.50"),  "numbers keep their text");
        Assert.That(value.Extra["extra"]!["n"]![2]!.ToJson(),     Is.EqualTo("1e400"), "even beyond double");
        Assert.That(value.ToJson(),                               Is.EqualTo(JSON),    "round trip");
        Assert.That(Overflowing.FromJson(Encoding.UTF8.GetBytes(JSON)).ToJson(), Is.EqualTo(JSON), "UTF-8 round trip");
        Assert.That(Overflowing.FromJson("""{"Name":"y"}""").Extra, Is.Null, "nothing to capture: the bag isn't created");

        Overflowing built = new()
                            {
                                Name  = "y",
                                Extra = new OrderedDictionary<string, JNode?> { ["e"] = JNode.Parse("""{ "k" : [ 1 , "v" ] }"""), ["z"] = null }
                            };

        Assert.That(built.ToJson(), Is.EqualTo("""{"Name" : "y","e" : {"k" : [1,"v"]},"z" : null}"""), "written after the declared members");
    }

    [Test] public void Overflow_GetOnlyBag_AndInitOnlyBag()
    {
        OverflowingNode node = OverflowingNode.FromJson("""{"id":1,"a":"b","c":[2]}""");
        Assert.That(node.Id,                Is.EqualTo(1));
        Assert.That(node.Rest.Count,        Is.EqualTo(2), "added to the bag the type created");
        Assert.That(node.ToJson(),          Is.EqualTo("""{"id" : 1,"a" : "b","c" : [2]}"""));

        OverflowingRecord record = OverflowingRecord.FromJson("""{"Name":"n","more":1.5}""");
        Assert.That(record.Rest!["more"]!.ToJson(), Is.EqualTo("1.5"));
        Assert.That(record.ToJson(),               Is.EqualTo("""{"Name" : "n","more" : 1.5}"""));
    }

    [Test] public void TypeDefaults_ApplyToItsOwnOutput()
    {
        Pretty value = new() { Values = [1, 2] };
        Assert.That(value.ToJson(),            Is.EqualTo("{\n    \"Values\" : [\n        1,\n        2\n    ]\n}"));
        Assert.That(value.ToString(),          Is.EqualTo("custom"),                 "GenerateToString = Off keeps the user's ToString");
        Assert.That(value.ToString("c", null), Is.EqualTo("""{"Values" : [1,2]}"""), "format c = compact");
    }


    // ─── Polymorphism ────────────────────────────────────────────────────────

    [Test] public void Polymorphism_WritesTheDiscriminatorFirst_AndReadsAnyOrder()
    {
        Zoo zoo = new()
                  {
                      Animals =
                      [
                          new Dog
                          {
                              Name    = "Rex",
                              GoodBoy = true
                          },
                          new Cat { Name = "Tom" },
                          new Puppy
                          {
                              Name  = "Bit",
                              Weeks = 8
                          }
                      ],
                      Star = new Cat
                             {
                                 Name  = "Star",
                                 Lives = 3
                             }
                  };

        string json = zoo.ToJson();
        Assert.That(json, Is.EqualTo("""{"Animals" : [{"kind" : "dog","Name" : "Rex","GoodBoy" : true},{"kind" : "cat","Name" : "Tom","Lives" : 9},{"kind" : "puppy","Name" : "Bit","GoodBoy" : false,"Weeks" : 8}],"Star" : {"kind" : "cat","Name" : "Star","Lives" : 3}}"""));

        Zoo back = Zoo.FromJson(json);
        Assert.That(back.Animals[0],                  Is.TypeOf<Dog>());
        Assert.That(back.Animals[1],                  Is.TypeOf<Cat>());
        Assert.That(back.Animals[2],                  Is.TypeOf<Puppy>());
        Assert.That(( (Puppy)back.Animals[2] ).Weeks, Is.EqualTo(8));
        Assert.That(back.ToJson(),                    Is.EqualTo(json));

        // The discriminator doesn't have to come first when reading.
        Animal late = Animal.FromJson("""{"Name":"Late","Lives":2,"kind":"cat"}""");
        Assert.That(late,                Is.TypeOf<Cat>());
        Assert.That(( (Cat)late ).Lives, Is.EqualTo(2));
    }

    [Test] public void Polymorphism_UnknownOrMissingDiscriminator_Fails()
    {
        Assert.That(Animal.TryFromJson("""{"kind":"cow","Name":"x"}""", out _), Is.False);
        Assert.That(Animal.TryFromJson("""{"Name":"x"}""",              out _), Is.False,        "abstract base without a discriminator");
        Assert.That(Cat.TryFromJson("""{"kind":"dog","Name":"x"}""", out _),    Is.False,        "a derived type checks its own tag");
        Assert.That(Cat.FromJson("""{"Name":"x"}""").Name,                      Is.EqualTo("x"), "a derived type doesn't need the tag");
    }


    // ─── Extension data, converters, nodes ───────────────────────────────────

    [Test] public void ExtensionData_CapturesUnknownMembers_InOrder()
    {
        const string JSON = """{"Title" : "t","z" : [1,2],"a" : {"b" : null}}""";

        Bag bag = Bag.FromJson(JSON);
        Assert.That(bag.Extra!.Count,       Is.EqualTo(2));
        Assert.That(bag.Extra.GetAt(0).Key, Is.EqualTo("z"));
        Assert.That(bag.ToJson(),           Is.EqualTo(JSON));
    }

    [Test] public void CustomConverter_AndNodeMembers()
    {
        Converted value = new()
                          {
                              When  = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000),
                              Maybe = null,
                              Any   = JNode.Parse("""{"x":[1,"two",null]}"""),
                              Array = new JArrayNode(1, true, "s")
                          };

        string json = value.ToJson();
        Assert.That(json, Is.EqualTo("""{"When" : 1700000000,"Maybe" : null,"Any" : {"x" : [1,"two",null]},"Array" : [1,true,"s"]}"""));

        Converted back = Converted.FromJson(json);
        Assert.That(back.When,                                        Is.EqualTo(value.When));
        Assert.That(back.Maybe,                                       Is.Null);
        Assert.That(JNode.DeepEquals(back.Any, value.Any),            Is.True);
        Assert.That(back.ToJson(),                                    Is.EqualTo(json));
        Assert.That(Converted.TryFromJson("""{"Array":{}}""", out _), Is.False, "a JArrayNode member rejects an object");
    }


    // ─── Structs, generics, constructors, fields ─────────────────────────────

    [Test] public void Struct_WithFields()
    {
        Point point = new()
                      {
                          X = 3,
                          Y = 4
                      };

        Assert.That(point.ToJson(), Is.EqualTo("""{"X" : 3,"Y" : 4,"Sum" : 7}"""));

        Point back = Point.FromJson("""{"X":3,"Y":4,"Sum":100}""");
        Assert.That(back.X + back.Y, Is.EqualTo(7), "Sum is computed: written, never read");
    }

    [Test] public void RecordStruct_WithOptionalConstructorParameter()
    {
        Assert.That(Money.FromJson("""{"Amount":1.5}""").Currency,         Is.EqualTo("USD"));
        Assert.That(Money.FromJson("""{"Amount":1.5,"Currency":"EUR"}"""), Is.EqualTo(new Money(1.5m, "EUR")));
        Assert.That(Money.TryFromJson("""{"Currency":"EUR"}""", out _),    Is.False, "Amount is required");
    }

    [Test] public void GenericModels_DispatchStatically()
    {
        Page<LineItem> page = new()
                              {
                                  Items = [new LineItem("a", 1, 1m)],
                                  Total = 1
                              };

        string json = page.ToJson();

        Assert.That(Page<LineItem>.FromJson(json).Items[0].Sku, Is.EqualTo("a"));

        Keyed<Guid> keyed = new()
                            {
                                Single = Guid.Empty,
                                Counts = new Dictionary<string, int> { ["x"] = 1 }
                            };

        Assert.That(Keyed<Guid>.FromJson(keyed.ToJson()).Single, Is.EqualTo(Guid.Empty));
    }

    [Test] public void JsonConstructor_AndRequiredAttribute()
    {
        Account account = Account.FromJson("""{"Login":"ada","Age":36,"Email":"a@b.c"}""");
        Assert.That(account.Login,                                              Is.EqualTo("ada"));
        Assert.That(account.Age,                                                Is.EqualTo(36));
        Assert.That(Account.TryFromJson("""{"Login":"ada","Age":36}""", out _), Is.False, "Email is [JsonMember(Required = true)]");
    }

    [Test] public void TypeCodec_WritesIntoAnOpenWriter()
    {
        Assert.That(JsonCodecRegistry.TryGet(typeof(Shorthand),       out JsonTypeCodec? model), Is.True);
        Assert.That(JsonCodecRegistry.TryGet(typeof(List<Shorthand>), out JsonTypeCodec? list),  Is.True);

        const string ITEM = """{"First" : 0,"full_name" : "","Internal" : 0,"Code" : "c","wins" : 0,"Kept" : 0}""";
        JsonWriter   writer = new(JsonWriterOptions.Default);

        try
        {
            writer.WriteStartObject();
            writer.WritePropertyName("a");
            model!.WriteValue(new Shorthand { Code = "c" }, ref writer);
            writer.WritePropertyName("b");
            model.WriteValue(null, ref writer);
            writer.WritePropertyName("c");
            list!.WriteValue(new List<Shorthand> { new() { Code = "c" } }, ref writer);
            writer.WriteEndObject();

            Assert.That(writer.ToString(), Is.EqualTo($$"""{"a" : {{ITEM}},"b" : null,"c" : [{{ITEM}}]}"""));
        }
        finally { writer.Dispose(); }
    }

    [Test] public void MemberAttributes_AreHonored()
    {
        Shorthand value = new()
                          {
                              Name     = "Ada",
                              First    = 1,
                              Secret   = "s",
                              Optional = null,
                              Zero     = 0,
                              Internal = 7,
                              Code     = "c",
                              Both     = 3,
                              Kept     = 4
                          };

        string json = value.ToJson();

        Assert.That(json, Is.EqualTo("""{"First" : 1,"full_name" : "Ada","Internal" : 7,"Code" : "c","wins" : 3,"Kept" : 4}"""));
        Assert.That(new Shorthand { Optional = "o", Zero = 2, Code = "c" }.ToJson(), Does.Contain("\"Optional\" : \"o\"").And.Contain("\"Zero\" : 2"), "WhenNull / WhenDefault only skip null / default");

        Shorthand back = Shorthand.FromJson(json);
        Assert.That(back.Secret,   Is.EqualTo("hidden"));
        Assert.That(back.Internal, Is.EqualTo(7));
        Assert.That(back.Kept,     Is.EqualTo(4), "[JsonMember(Ignore = false)] overrides [JsonSkip]");
        Assert.That(Shorthand.FromJson("""{"Code":"c","Secret":"s"}""").Secret, Is.EqualTo("hidden"), "a skipped member is never read");
        Assert.That(Shorthand.TryFromJson("""{"full_name":"x"}""", out _),     Is.False,              "Code is [JsonMandatory]");
    }

    [Test] public void NestedType_KebabCase()
    {
        Outer.Inner inner = new("a", null);
        Assert.That(inner.ToJson(),                                                   Is.EqualTo("""{"first-value" : "a","second-value" : null}"""));
        Assert.That(Outer.Inner.FromJson("""{"first-value":"a","second-value":2}"""), Is.EqualTo(new Outer.Inner("a", 2)));
    }


    // ─── ISpanFormattable / ISpanParsable ────────────────────────────────────

    [Test] public void SpanFormattable_WritesIntoTheCallerBuffer()
    {
        LineItem   item   = new("a", 1, 2m);
        Span<char> buffer = stackalloc char[256];

        Assert.That(item.TryFormat(buffer, out int written, default, null), Is.True);
        Assert.That(buffer[..written].ToString(),                           Is.EqualTo(item.ToJson()));

        Assert.That(item.TryFormat(stackalloc char[10], out written, default, null), Is.False);
        Assert.That(written,                                                         Is.Zero);

        Span<byte> bytes = stackalloc byte[256];
        Assert.That(item.TryFormat(bytes, out int bytesWritten, "i", null), Is.True);
        Assert.That(Encoding.UTF8.GetString(bytes[..bytesWritten]),         Is.EqualTo(item.ToJson(JsonWriterOptions.Indent)));
    }

    [Test] public void SpanParsable_GenericCode()
    {
        Assert.That(Parse<LineItem>("""{"sku":"a","quantity":1,"price":2}""").Sku,  Is.EqualTo("a"));
        Assert.That(LineItem.TryParse("nope", CultureInfo.InvariantCulture, out _), Is.False);
        Assert.Throws<JsonReadException>(() => LineItem.Parse("{", null));

        static T Parse<T>( string text )
            where T : ISpanParsable<T> => T.Parse(text.AsSpan(), CultureInfo.InvariantCulture);
    }

    [Test] public void FormatString_Rejected() { Assert.Throws<FormatException>(() => new LineItem("a", 1, 1m).ToString("x", null)); }
}
