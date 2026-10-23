# Jakar.Json

A reflection-free, allocation-free JSON serializer. `[JsonModel]` source-generates readers and writers for UTF-16 and UTF-8 that implement `ISpanFormattable` / `ISpanParsable<T>` (and the UTF-8 versions). The package also has a zero-allocation JSON tape and a Newtonsoft-style DOM. It's standalone: there's no dependency on, or interop with, `System.Text.Json` (hosts built on it are covered by the [integration packages](https://github.com/Jakar510/Jakar.Json#packages)). The design is in [SPEC.md](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/SPEC.md).

```csharp
using Jakar.Extensions;   // [JsonModel]
using Jakar.Json;         // JsonMember, JsonKey, JsonNaming, JsonCodec, ...

[JsonModel(Naming = JsonNaming.CamelCase)]
public sealed partial record Invoice( Guid Id, decimal Total )
{
    public required List<LineItem> Lines { get; init; }

    [JsonKey("memo"), JsonSkip(Condition = JsonSkipCondition.WhenNull)]
    public string? Notes { get; init; }

    public decimal Average => Total / Lines.Count;   // computed: written, never read (ReadOnlyMembers = Ignore leaves it out)

    [JsonOverflow] public JObjectNode? Extra { get; init; }   // members the type doesn't declare, kept for the round trip
}

string  json    = invoice.ToJson();                // {"id" : "…","total" : 1.10,"lines" : [...]}
byte[]  utf8    = invoice.ToJsonUtf8();
Invoice back    = Invoice.FromJson(json);          // or FromJson(ReadOnlySpan<byte>), FromJson(Stream), FromJsonAsync(...)
bool    ok      = Invoice.TryFromJson(json, out Invoice? maybe);
bool    written = invoice.TryFormat(buffer, out int chars, default, null);   // ISpanFormattable: no allocation
T       parsed  = T.Parse(text, null);                                       // ISpanParsable<T> in generic code
```

## What you get

- **No reflection.** Everything is generated at compile time: member access, construction, enum names, polymorphic dispatch. Converters are static interfaces (`IJsonConverter<T>`), so generic code is specialized by the JIT. It publishes with Native AOT and full trimming with no warnings.
- **No incidental allocations.**
  - Writing to a span, an `IBufferWriter` or a `Stream` allocates nothing.
  - `ToJson()` allocates only the result string.
  - Reading allocates only the result graph. Collections are allocated once, at their exact size.
  - The test suite checks these budgets exactly (`AllocationTests.cs`).
- **Idempotent.**
  - The same value always produces the same bytes: on any machine, OS, culture or thread, and whatever order its dictionaries and sets were filled in.
  - Reading and re-writing any document reaches one canonical form in a single pass.
  - Canonical numbers: the shortest round-trippable form, an exponent like `1e+20`, and `decimal` keeps its scale.
- **Strict and bounded.** Reading follows RFC 8259 by default. Depth is limited, and nesting is tracked without recursion, so a deeply nested document can't overflow the stack. Invalid UTF-8 and unpaired surrogates are rejected. Errors give the position, line, column and JSON path (`$.lines[3].price`).
- **Both encodings natively.** `JsonWriter` (UTF-16, on `ValueStringBuilder`), `JsonUtf8Writer` (UTF-8, on `ValueUtf8Builder`, or streaming to a `Stream`), and `JsonReader<char>` / `JsonReader<byte>` (on `ValueSpanReader<T>`).

## Settings

Settings resolve from the nearest level: `[JsonMember]` on the member, then `[JsonModel]` on the type, then `[assembly: JsonDefaults]`, then the built-in default. They're compiled into the generated code. A nested type always uses its own settings, so a type's JSON doesn't depend on what contains it.

| Setting | Default | Options |
|---|---|---|
| `Naming` / `EnumNaming` | `AsDeclared` | `CamelCase`, `PascalCase`, `SnakeCaseLower/Upper`, `KebabCaseLower/Upper` |
| `NameMatching` | `Exact` | `OrdinalIgnoreCase` |
| `NullValues` / `DefaultValues` | `Write` | `Omit` |
| `UnknownMembers` | `Skip` | `Error`, `Capture` (into a `[JsonOverflow]` member; having one implies `Capture`) |
| `DuplicateMembers` | `Error` | `LastWins` |
| `Enums` | `Name` | `Number` (reading accepts names and numbers either way) |
| `NumbersFromStrings` | `Disallow` | `Allow` |
| `LargeIntegers` | `Number` | `String` (beyond ±2^53, for JavaScript) |
| `NonFiniteFloats` | `AsString` (`"NaN"`, `"Infinity"`, `"-Infinity"`) | `Error` (JSON numbers only) |
| `LocalDateTimes` | `ConvertToUtc` | `WriteOffset`, `Error` |
| `UnorderedCollections` | `Sorted` | `Enumeration` |
| `ReadOnlyMembers` | `Write` | `Ignore`: get-only members no constructor parameter binds (computed properties) are left out. Per member: `[JsonMember(ReadOnly = ...)]` |
| `Escaping` | `Minimal` | `AsciiOnly`, `HtmlSafe` |
| `Indented`, `IndentChar`, `IndentSize` | off, tab, 1 | — |
| `AllowComments`, `AllowTrailingCommas` | off | — |
| `MaxDepth` | 64 | up to 1024 |
| `GenerateToString` | on | — |

Output is compact unless indented, with `" : "` between names and values: `{"id" : 1,"name" : "x"}`. Indentation is one tab per level by default.

## Members, constructors and types

- **Which members.** Public properties and fields are included, plus anything marked `[JsonMember]`, `[JsonKey]` or `[JsonOptIn]`. Members are ordered by `Order`, then base types first, then declaration order.
- **Member attributes.** `[JsonMember(...)]` sets everything at once; the single-purpose attributes are shorthands, and `[JsonMember]`'s explicit values win over them:

  | Attribute | Same as |
  |---|---|
  | `[JsonKey("name")]` | `[JsonMember(Name = "name")]` |
  | `[JsonOrder(n)]` | `[JsonMember(Order = n)]` |
  | `[JsonSkip]` | `[JsonMember(Ignore = true)]`; `Condition = WhenNull` / `WhenDefault` only skips writing null / default |
  | `[JsonMandatory]` | `[JsonMember(Required = true)]` |
  | `[JsonOptIn]` | includes a non-public member or field |
  | `[JsonOverflow]` | `[JsonMember(ExtensionData = true)]` |
  | `[JsonCreator]` | (on a constructor) the one to read through |
  | `[JsonEnumName("x")]` | (on an enum value) its JSON name |

- **Extension data.** A `[JsonOverflow]` member (`OrderedDictionary<string, JNode?>` or `JObjectNode`) collects the members the type doesn't declare, in input order, numbers keeping their text, and writes them back after the declared members. It may be settable, init-only, or get-only with an instance the type creates.
- **Computed members.** A get-only member that no constructor parameter binds is written but never read (JJSON011). `ReadOnlyMembers = Ignore` on `[JsonModel]` or `[assembly: JsonDefaults]` leaves such members out; `[JsonMember(ReadOnly = JsonReadOnlyMembers.Write / Ignore)]` decides per member.
- **NaN and infinities.** By default `NaN`, `Infinity` and `-Infinity` are written as those strings and read back (float, double, Half; not decimal), in models, collections, dictionaries and the DOM. `NonFiniteFloats = Error` makes them an error instead.
- **Construction.** The generator uses, in order: the constructor marked `[JsonCreator]`, the primary constructor, the single public constructor, or the parameterless one. Parameters bind to members by name. Every other member is set only when it's present in the JSON, so property initializers survive a missing member. Init-only members are set through `[UnsafeAccessor]`, which is resolved at compile time.
- **Required members.** `required` members, constructor parameters without defaults, and `[JsonMandatory]` / `[JsonMember(Required = true)]` members must be present when reading.
- **Supported types:**
  - all primitives, including `Int128`, `UInt128`, `BigInteger` and `Half`;
  - `string`, `char`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`, `Uri` and `Version`;
  - enums (with `[Flags]` written as `"A, B"`) and `Nullable<T>`;
  - other `[JsonModel]` types Jakar.Json implements, and any `ISpanFormattable` + `ISpanParsable<T>` type;
  - arrays, `List<T>`, the list and collection interfaces, the immutable, frozen, sorted and hash-based sets, `Queue<T>` and `Stack<T>`;
  - every dictionary type, keyed by `string`, an enum, or any parsable type;
  - any `IJsonCollection<TSelf, T>`: a collection that writes its own elements and is built from a span in one call (the `Jakar.Extensions` observable collections write their unfiltered contents this way);
  - `JNode` and its subtypes.
- **Custom converters.** `[JsonMember(Converter = typeof(MyConverter))]`, where the converter is a `struct` implementing `IJsonConverter<T>`.
- **Polymorphism.** `[JsonDerived(typeof(Dog), "dog")]` on the base type. The discriminator (`"$type"` by default) is written first, and when reading it may appear anywhere in the object.
- **Generics.** Type parameters work when constrained to `IJsonSerializable<T>`, or to `ISpanFormattable` + `ISpanParsable<T>`.
- **Migrating from System.Text.Json.** Its attributes aren't read. Replace `JsonPropertyName` → `JsonKey`, `JsonPropertyOrder` → `JsonOrder`, `JsonIgnore` → `JsonSkip` (`WhenWritingNull` → `WhenNull`, `WhenWritingDefault` → `WhenDefault`), `JsonRequired` → `JsonMandatory`, `JsonInclude` → `JsonOptIn`, `JsonExtensionData` → `JsonOverflow`, `JsonConstructor` → `JsonCreator`, `JsonStringEnumMemberName` → `JsonEnumName`, `JsonDerivedType` → `JsonDerived`.

## Helpers

`JsonCodec` has the generic versions of every generated helper, plus:

- **Root arrays:** `ToJsonArray`, `FromJsonArray`, `FromJsonList`, and `FromJsonArrayPooled`, which returns a pooled buffer you dispose.
- **NDJSON:** `ReadLines<T>` / `ReadLinesAsync<T>` and `WriteLines<T>`. Memory is bounded by the longest line.
- **Inputs and outputs:** `ReadOnlySequence<byte>`, `TextReader` / `TextWriter`, appending to a `ValueStringBuilder`, and any buffer writer: `WriteJson(value, ref writer)` (UTF-8) and `WriteJsonUtf16(value, ref writer)` take a generic `TWriter : IBufferWriter<…>` by `ref`, so struct and `ref struct` writers work without boxing.
- **By `Type` at run time:** `JsonCodecRegistry.TryGet(type, out JsonTypeCodec codec)`, then `codec.Read(...)` / `codec.Write(value, ref bufferWriter)`. For frameworks that pick a serializer per type. Each assembly's generated module initializer registers its non-generic `[JsonModel]` types, and lookups also resolve `T[]`, `List<T>` and the common collection interfaces of them. No reflection. [Jakar.Json.AspNetCore](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.AspNetCore/README.md) uses it for MVC and minimal APIs.

## Dynamic JSON

- **`JsonTape`** parses into a pooled index. Navigating it allocates nothing: `tape.Root["lines"][3]["price"].GetDecimal()`, `EnumerateArray()`, `ValueEquals("paid")`. `Deserialize<T>()` reads a model straight from the index.
- **`JNode`** (`JObjectNode`, `JArrayNode`, `JValueNode`) is a mutable tree:
  - Indexers return `null` for both a missing member and JSON null. Implicit and explicit conversions cover the common types.
  - Parsed numbers keep their original text.
  - `DeepClone`, `DeepEquals`, `Detach` and `Path` are available.
  - `ToModel<T>()` and `FromModel<T>()` convert without going through text.

## Diagnostics

| ID | Severity | Meaning |
|---|---|---|
| JJSON001 | Error | The type, or a containing type, isn't `partial` (code fix) |
| JJSON002 | Error | A type parameter needs `IJsonSerializable<T>` or `ISpanFormattable, ISpanParsable<T>` |
| JJSON003 | Error | A member's type isn't supported |
| JJSON004 | Error | No usable constructor, or a constructor parameter matches no member |
| JJSON005 | Error | Two members have the same JSON name |
| JJSON006 | Warning | Members span several files without explicit `Order` (code fix freezes the order) |
| JJSON007 | Info | A generated member was already declared by hand, so it's not generated |
| JJSON008 | Error | Contradictory settings |
| JJSON009 | Error | A converter doesn't fit its member |
| JJSON010 | — | Retired (side-by-side mode, before `[GenerateJson]` merged into `[JsonModel]`) |
| JJSON011 | Info | A member is written but never read (no setter, no constructor parameter); `ReadOnlyMembers = Ignore` leaves it out |
| JJSON012 | Error | `object`, delegate, pointer or ref struct member |
| JJSON013 | Warning | A setting weakens deterministic output |
| JJSON014 | Error | Invalid `[JsonDerived]` |
| JJSON015 | Error | `IJsonSerializable<T>` listed without `[JsonModel]` (code fix) |
| JJSON016, JJSON017 | — | Retired (System.Text.Json contexts, removed in 10.3.0) |

## Notes

- **Errors.** Reading throws `JsonReadException` (`Error`, `Path`, `LineNumber`, `BytePositionInLine`); writing a value JSON can't hold throws `JsonWriteException`.
- The only reflection-adjacent code is in the BCL collections the reader creates, such as `Dictionary<TKey, TValue>` with its default comparer. Jakar.Json itself makes no reflection calls, and writing to sets and dictionaries uses comparisons chosen at compile time.
- `byte[]` is written as a JSON array of numbers, not base64. Use a converter if you need base64.
- Object references aren't preserved. A cyclic graph fails at `MaxDepth` with `DepthExceeded`.
- Settings come only from the arguments written on an attribute: a setting you don't set falls through to the next level, so the defaults in the table above apply.
- **ASP.NET Core:** [Jakar.Json.AspNetCore](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.AspNetCore/README.md) makes Jakar.Json the serializer for `[JsonModel]` types in MVC and minimal APIs.
- **Versions** follow `<.NET major>.<library major>.<library minor>` (see the [repository README](https://github.com/Jakar510/Jakar.Json#versioning)).
