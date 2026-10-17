// Jakar.Json.OpenApi
// 10/17/2026

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Jakar.Extensions;



namespace Jakar.Json.OpenApi;


/// <summary> A member of a <c>[JsonModel]</c> type as Jakar.Json writes it: its JSON name, and whether it's written (readable) and read (writable). </summary>
public sealed record JsonModelMember( MemberInfo Member, string Name, Type MemberType, bool IsWritten, bool IsRead );



/// <summary>
///     The JSON members of <c>[JsonModel]</c> types, resolved by the rules the Jakar.Json generator applies at compile time: public properties and fields, plus members marked
///     <c>[JsonMember]</c>, <c>[JsonPropertyName]</c> or <c>[JsonInclude]</c>; minus <c>[JsonMember(Ignore = true)]</c>, <c>[JsonIgnore]</c> and extension data; named by
///     <c>[JsonMember(Name)]</c>, <c>[JsonPropertyName]</c>, or the naming policy from <c>[JsonModel]</c>, the type's System.Text.Json context, or <c>[assembly: JsonDefaults]</c>.
/// </summary>
/// <remarks> Settings come only from arguments written on the attributes (as in the generator), so they're read with <see cref="CustomAttributeData"/>, not attribute instances. </remarks>
public static class JsonModelContract
{
    private const string STJ = "System.Text.Json.Serialization.";

    private static readonly ConcurrentDictionary<Type, IReadOnlyList<JsonModelMember>?> __members = new();


    /// <summary> Whether <paramref name="type"/> is a <c>[JsonModel]</c> type whose code Jakar.Json generates (not one backed by System.Text.Json only). </summary>
    public static bool IsJsonModel( Type type ) => GetModelAttribute(type) is not null && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IJsonSerializable<>) && i.GenericTypeArguments[0] == type);

    /// <summary> The members Jakar.Json writes and reads for <paramref name="type"/>, in declaration order (base types first); <see langword="null"/> when it isn't a <c>[JsonModel]</c> type. </summary>
    public static IReadOnlyList<JsonModelMember>? GetMembers( Type type ) => __members.GetOrAdd(type, static t => IsJsonModel(t) ? Resolve(t) : null);


    private static CustomAttributeData? GetModelAttribute( Type type ) => type.GetCustomAttributesData().FirstOrDefault(static a => a.AttributeType == typeof(JsonModelAttribute));

    private static IReadOnlyList<JsonModelMember> Resolve( Type type )
    {
        ( JsonNaming naming, bool includeFields ) = ResolveSettings(type);

        // Most-derived first, so an override or 'new' member hides the base one; base members come first in the output.
        List<Type> chain = [];
        for ( Type? current = type; current is not null && current != typeof(object) && current != typeof(ValueType); current = current.BaseType ) { chain.Add(current); }

        HashSet<string>                    names  = new(StringComparer.Ordinal);
        List<(int Depth, JsonModelMember)> result = [];

        for ( int level = 0; level < chain.Count; level++ )
        {
            bool own = level == 0;

            foreach ( MemberInfo member in chain[level].GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).OrderBy(static m => m.MetadataToken) )
            {
                if ( member is not (PropertyInfo or FieldInfo) || !names.Add(member.Name) ) { continue; }

                if ( Build(member, own, naming, includeFields) is { } built ) { result.Add(( chain.Count - 1 - level, built )); }
            }
        }

        return result.OrderBy(static x => x.Depth).Select(static x => x.Item2).ToArray();
    }

    private static JsonModelMember? Build( MemberInfo member, bool own, JsonNaming naming, bool includeFields )
    {
        Type type;
        bool isPublic, canRead, canWrite;

        switch ( member )
        {
            case PropertyInfo property:
                if ( property.GetIndexParameters().Length > 0 || property.Name.Contains('.') ) { return null; } // indexers, explicit interface implementations

                MethodInfo? getter = property.GetMethod;
                MethodInfo? setter = property.SetMethod;
                type     = property.PropertyType;
                isPublic = getter?.IsPublic is true || setter?.IsPublic is true;
                canRead  = getter is not null && ( own || !getter.IsPrivate );
                canWrite = setter is not null && ( own || !setter.IsPrivate );
                break;

            case FieldInfo field:
                if ( field.IsLiteral || field.IsDefined(typeof(CompilerGeneratedAttribute)) ) { return null; } // constants, backing fields

                type     = field.FieldType;
                isPublic = field.IsPublic;
                canRead  = true;
                canWrite = true; // readonly fields are set through [UnsafeAccessor]
                break;

            default: return null;
        }

        Attributes attributes = Attributes.Read(member);
        if ( attributes.Ignore || attributes.ExtensionData ) { return null; }

        if ( !isPublic && ( !attributes.Marked || ( !own && IsPrivate(member) ) ) ) { return null; }

        if ( member is FieldInfo && !includeFields && !attributes.Marked ) { return null; }

        return new JsonModelMember(member, attributes.Name ?? Naming.Apply(member.Name, naming), type, canRead, canWrite);
    }

    private static bool IsPrivate( MemberInfo member ) => member switch
                                                          {
                                                              PropertyInfo p => ( p.GetMethod ?? p.SetMethod )?.IsPrivate is true,
                                                              FieldInfo f    => f.IsPrivate,
                                                              _              => false
                                                          };


    /// <summary> The naming policy and field inclusion: <c>[JsonModel]</c>, then the System.Text.Json context it names (or the assembly default context), then <c>[assembly: JsonDefaults]</c>. </summary>
    private static (JsonNaming Naming, bool IncludeFields) ResolveSettings( Type type )
    {
        CustomAttributeData model = GetModelAttribute(type)!;

        Type? context = model.ConstructorArguments.Count == 1 && model.ConstructorArguments[0].Value is Type explicitContext
                            ? explicitContext
                            : type.Assembly.GetCustomAttributesData().FirstOrDefault(static a => a.AttributeType.FullName == "Jakar.Extensions.JsonModelContextAttribute") is { ConstructorArguments.Count: 1 } assemblyContext && model.ConstructorArguments.Count == 0 && UsesAssemblyContext(type)
                                ? assemblyContext.ConstructorArguments[0].Value as Type
                                : null;

        JsonNaming? typeNaming = Named<JsonNaming>(model, "Naming");
        if ( typeNaming is JsonNaming.Inherit ) { typeNaming = null; }

        if ( context is not null )
        {
            ( JsonNaming contextNaming, bool includeFields ) = MirrorContext(context);
            return ( typeNaming ?? contextNaming, includeFields );
        }

        JsonNaming? assemblyNaming = type.Assembly.GetCustomAttributesData().FirstOrDefault(static a => a.AttributeType == typeof(JsonDefaultsAttribute)) is { } defaults
                                         ? Named<JsonNaming>(defaults, "Naming")
                                         : null;

        if ( assemblyNaming is JsonNaming.Inherit ) { assemblyNaming = null; }

        return ( typeNaming ?? assemblyNaming ?? JsonNaming.AsDeclared, true );
    }

    /// <summary> A context-less <c>[JsonModel]</c> uses the assembly's default context only when it implements <c>IJsonModel&lt;T&gt;</c> (the System.Text.Json side). </summary>
    private static bool UsesAssemblyContext( Type type ) => type.GetInterfaces().Any(static i => i.IsGenericType && i.GetGenericTypeDefinition().FullName == "Jakar.Extensions.IJsonModel`1");

    /// <summary> <c>[JsonSourceGenerationOptions]</c> on the context: Web defaults are camelCase; <c>PropertyNamingPolicy</c> and <c>IncludeFields</c> override. </summary>
    private static (JsonNaming Naming, bool IncludeFields) MirrorContext( Type context )
    {
        CustomAttributeData? options = context.GetCustomAttributesData().FirstOrDefault(static a => a.AttributeType.FullName == STJ + "JsonSourceGenerationOptionsAttribute");
        if ( options is null ) { return ( JsonNaming.AsDeclared, false ); }

        bool       web           = options.ConstructorArguments.Count == 1 && Convert.ToInt32(options.ConstructorArguments[0].Value) == 1;
        JsonNaming naming        = web ? JsonNaming.CamelCase : JsonNaming.AsDeclared;
        bool       includeFields = false;

        foreach ( CustomAttributeNamedArgument argument in options.NamedArguments )
        {
            switch ( argument.MemberName )
            {
                case "PropertyNamingPolicy":
                    naming = Convert.ToInt32(argument.TypedValue.Value) switch
                             {
                                 1 => JsonNaming.CamelCase,
                                 2 => JsonNaming.SnakeCaseLower,
                                 3 => JsonNaming.SnakeCaseUpper,
                                 4 => JsonNaming.KebabCaseLower,
                                 5 => JsonNaming.KebabCaseUpper,
                                 _ => naming
                             };

                    break;

                case "IncludeFields":
                    includeFields = argument.TypedValue.Value is true;
                    break;
            }
        }

        return ( naming, includeFields );
    }

    private static T? Named<T>( CustomAttributeData attribute, string name )
        where T : struct, Enum
    {
        foreach ( CustomAttributeNamedArgument argument in attribute.NamedArguments )
        {
            if ( argument.MemberName == name && argument.TypedValue.Value is { } value ) { return (T)Enum.ToObject(typeof(T), value); }
        }

        return null;
    }



    /// <summary> The attributes Jakar.Json reads on a member: its own, plus System.Text.Json's (as the generator does). </summary>
    private readonly record struct Attributes( bool Ignore, bool Marked, string? Name, bool ExtensionData )
    {
        public static Attributes Read( MemberInfo member )
        {
            bool    ours = false, ourIgnore = false, stjIgnore = false, marked = false, extension = false;
            string? name = null, stjName = null;

            foreach ( CustomAttributeData attribute in member.GetCustomAttributesData() )
            {
                if ( attribute.AttributeType == typeof(JsonMemberAttribute) )
                {
                    ours   = true;
                    marked = true;

                    foreach ( CustomAttributeNamedArgument argument in attribute.NamedArguments )
                    {
                        switch ( argument.MemberName )
                        {
                            case "Name":
                                name = argument.TypedValue.Value as string;
                                break;

                            case "Ignore":
                                ourIgnore = argument.TypedValue.Value is true;
                                break;

                            case "ExtensionData":
                                extension |= argument.TypedValue.Value is true;
                                break;
                        }
                    }

                    continue;
                }

                switch ( attribute.AttributeType.FullName )
                {
                    case STJ + "JsonPropertyNameAttribute" when attribute.ConstructorArguments.Count == 1:
                        marked  = true;
                        stjName = attribute.ConstructorArguments[0].Value as string;
                        break;

                    case STJ + "JsonExtensionDataAttribute":
                        marked    = true;
                        extension = true;
                        break;

                    case STJ + "JsonIncludeAttribute":
                        marked = true;
                        break;

                    case STJ + "JsonIgnoreAttribute":
                        // JsonIgnoreCondition: Never = 0, Always = 1 (the default), WhenWritingDefault = 2, WhenWritingNull = 3.
                        int condition = attribute.NamedArguments.FirstOrDefault(static a => a.MemberName == "Condition").TypedValue.Value is { } c
                                            ? Convert.ToInt32(c)
                                            : 1;

                        stjIgnore = condition == 1;
                        break;
                }
            }

            // [JsonMember(Ignore = false)] overrides an STJ [JsonIgnore]; [JsonMember(Ignore = true)] always wins.
            bool ignore = ours
                              ? ourIgnore
                              : stjIgnore;

            return new Attributes(ignore, marked, name ?? stjName, extension);
        }
    }
}



/// <summary> The generator's naming policies (Jakar.Json.Generator/Naming.cs), so schema names match the generated code exactly. </summary>
internal static class Naming
{
    public static string Apply( string name, JsonNaming policy ) => policy switch
                                                                    {
                                                                        JsonNaming.CamelCase      => CamelCase(name),
                                                                        JsonNaming.PascalCase     => PascalCase(name),
                                                                        JsonNaming.SnakeCaseLower => Separated(name, '_', false),
                                                                        JsonNaming.SnakeCaseUpper => Separated(name, '_', true),
                                                                        JsonNaming.KebabCaseLower => Separated(name, '-', false),
                                                                        JsonNaming.KebabCaseUpper => Separated(name, '-', true),
                                                                        _                         => name
                                                                    };

    public static string CamelCase( string name )
    {
        if ( name.Length == 0 || !char.IsUpper(name[0]) ) { return name; }

        char[] chars = name.ToCharArray();

        for ( int i = 0; i < chars.Length; i++ )
        {
            if ( i == 1 && !char.IsUpper(chars[i]) ) { break; }

            bool hasNext = i + 1 < chars.Length;

            if ( i > 0 && hasNext && !char.IsUpper(chars[i + 1]) )
            {
                if ( chars[i + 1] == ' ' ) { chars[i] = char.ToLowerInvariant(chars[i]); }

                break;
            }

            chars[i] = char.ToLowerInvariant(chars[i]);
        }

        return new string(chars);
    }

    public static string PascalCase( string name ) => name.Length == 0 || char.IsUpper(name[0])
                                                          ? name
                                                          : char.ToUpperInvariant(name[0]) + name[1..];

    public static string Separated( string name, char separator, bool upper )
    {
        List<string>  words   = Words(name);
        StringBuilder builder = new(name.Length + words.Count);

        for ( int i = 0; i < words.Count; i++ )
        {
            if ( i > 0 ) { builder.Append(separator); }

            builder.Append(upper
                               ? words[i].ToUpperInvariant()
                               : words[i].ToLowerInvariant());
        }

        return builder.ToString();
    }

    private static List<string> Words( string name )
    {
        List<string>  words   = [];
        StringBuilder current = new();

        for ( int i = 0; i < name.Length; i++ )
        {
            char c = name[i];

            if ( !char.IsLetterOrDigit(c) )
            {
                Flush();
                continue;
            }

            if ( current.Length > 0 && char.IsUpper(c) )
            {
                char previous  = name[i - 1];
                bool nextLower = i + 1 < name.Length && char.IsLower(name[i + 1]);

                if ( char.IsLower(previous) || char.IsDigit(previous) || ( char.IsUpper(previous) && nextLower ) ) { Flush(); }
            }

            current.Append(c);
        }

        Flush();
        return words;

        void Flush()
        {
            if ( current.Length == 0 ) { return; }

            words.Add(current.ToString());
            current.Clear();
        }
    }
}
