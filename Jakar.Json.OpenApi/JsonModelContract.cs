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
///     <c>[JsonMember]</c>, <c>[JsonKey]</c> or <c>[JsonOptIn]</c>; minus <c>[JsonMember(Ignore = true)]</c>, <c>[JsonSkip]</c>, extension data (<c>[JsonOverflow]</c>) and, with
///     <c>ReadOnlyMembers = Ignore</c>, get-only members no constructor binds; named by <c>[JsonMember(Name)]</c>, <c>[JsonKey]</c>, or the naming policy from <c>[JsonModel]</c> or
///     <c>[assembly: JsonDefaults]</c>.
/// </summary>
/// <remarks>
///     Settings come only from arguments written on the attributes (as in the generator), so they're read with <see cref="CustomAttributeData"/>, not attribute instances. The
///     attributes are kept in metadata only when the model's assembly defines <c>JAKAR_JSON_KEEP_ATTRIBUTES</c>.
/// </remarks>
public static class JsonModelContract
{
    private const string J = "Jakar.Json.";

    private static readonly ConcurrentDictionary<Type, IReadOnlyList<JsonModelMember>?> __members = new();


    /// <summary> Whether <paramref name="type"/> is a <c>[JsonModel]</c> type whose code Jakar.Json generates. </summary>
    public static bool IsJsonModel( Type type ) => GetModelAttribute(type) is not null && type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IJsonSerializable<>) && i.GenericTypeArguments[0] == type);

    /// <summary> The members Jakar.Json writes and reads for <paramref name="type"/>, in declaration order (base types first); <see langword="null"/> when it isn't a <c>[JsonModel]</c> type. </summary>
    public static IReadOnlyList<JsonModelMember>? GetMembers( Type type ) => __members.GetOrAdd(type, static t => IsJsonModel(t) ? Resolve(t) : null);


    private static CustomAttributeData? GetModelAttribute( Type type ) => type.GetCustomAttributesData().FirstOrDefault(static a => a.AttributeType == typeof(JsonModelAttribute));

    private static IReadOnlyList<JsonModelMember> Resolve( Type type )
    {
        ( JsonNaming naming, JsonReadOnlyMembers readOnly ) = ResolveSettings(type);
        HashSet<string> bound = BoundParameters(type);

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

                if ( Build(member, own, naming, readOnly, bound) is { } built ) { result.Add(( chain.Count - 1 - level, built )); }
            }
        }

        return result.OrderBy(static x => x.Depth).Select(static x => x.Item2).ToArray();
    }

    private static JsonModelMember? Build( MemberInfo member, bool own, JsonNaming naming, JsonReadOnlyMembers readOnly, HashSet<string> bound )
    {
        Type type;
        bool isPublic, canRead, canWrite, settable;

        switch ( member )
        {
            case PropertyInfo property:
                if ( property.GetIndexParameters().Length > 0 || property.Name.Contains('.') ) { return null; } // indexers, explicit interface implementations

                MethodInfo? getter = property.GetMethod;
                MethodInfo? setter = property.SetMethod;
                type     = property.PropertyType;
                isPublic = getter?.IsPublic is true || setter?.IsPublic is true;
                canRead  = getter is not null && ( own || !getter.IsPrivate );
                settable = setter is not null && ( own || !setter.IsPrivate );
                canWrite = settable || bound.Contains(property.Name);
                break;

            case FieldInfo field:
                if ( field.IsLiteral || field.IsDefined(typeof(CompilerGeneratedAttribute)) ) { return null; } // constants, backing fields

                type     = field.FieldType;
                isPublic = field.IsPublic;
                canRead  = true;
                settable = !field.IsInitOnly;
                canWrite = true; // readonly fields are set through [UnsafeAccessor]
                break;

            default: return null;
        }

        Attributes attributes = Attributes.Read(member);
        if ( attributes.Ignore || attributes.ExtensionData ) { return null; }

        if ( !isPublic && ( !attributes.Marked || ( !own && IsPrivate(member) ) ) ) { return null; }

        // Get-only members no constructor parameter binds (computed properties): written, or left out with ReadOnlyMembers = Ignore.
        if ( !settable && !bound.Contains(member.Name) && ( attributes.ReadOnly ?? readOnly ) == JsonReadOnlyMembers.Ignore ) { return null; }

        return new JsonModelMember(member, attributes.Name ?? Naming.Apply(member.Name, naming), type, canRead, canWrite);
    }

    private static bool IsPrivate( MemberInfo member ) => member switch
                                                          {
                                                              PropertyInfo p => ( p.GetMethod ?? p.SetMethod )?.IsPrivate is true,
                                                              FieldInfo f    => f.IsPrivate,
                                                              _              => false
                                                          };

    /// <summary> Member names bound to a constructor parameter (ignoring case): the <c>[JsonCreator]</c> constructor's, otherwise every accessible constructor's. </summary>
    private static HashSet<string> BoundParameters( Type type )
    {
        ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        ConstructorInfo?  creator      = constructors.FirstOrDefault(static c => c.GetCustomAttributesData().Any(static a => a.AttributeType.FullName == J + "JsonCreatorAttribute"));

        IEnumerable<ConstructorInfo> candidates = creator is not null
                                                      ? [creator]
                                                      : constructors.Where(static c => c.IsPublic || c.IsAssembly);

        return new HashSet<string>(candidates.SelectMany(static c => c.GetParameters()).Select(static p => p.Name ?? ""), StringComparer.OrdinalIgnoreCase);
    }


    /// <summary> The naming policy and the read-only member handling: <c>[JsonModel]</c>, then <c>[assembly: JsonDefaults]</c>. </summary>
    private static (JsonNaming Naming, JsonReadOnlyMembers ReadOnly) ResolveSettings( Type type )
    {
        CustomAttributeData  model    = GetModelAttribute(type)!;
        CustomAttributeData? defaults = type.Assembly.GetCustomAttributesData().FirstOrDefault(static a => a.AttributeType == typeof(JsonDefaultsAttribute));

        JsonNaming naming = Set(Named<JsonNaming>(model, "Naming")) ?? ( defaults is null ? null : Set(Named<JsonNaming>(defaults, "Naming")) ) ?? JsonNaming.AsDeclared;

        JsonReadOnlyMembers readOnly = Set(Named<JsonReadOnlyMembers>(model, "ReadOnlyMembers")) ?? ( defaults is null ? null : Set(Named<JsonReadOnlyMembers>(defaults, "ReadOnlyMembers")) ) ?? JsonReadOnlyMembers.Write;

        return ( naming, readOnly );

        static T? Set<T>( T? value )
            where T : struct, Enum => value is { } v && Convert.ToInt32(v) != 0 // Inherit
                                          ? v
                                          : null;
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



    /// <summary> The attributes Jakar.Json reads on a member: <c>[JsonMember]</c>, whose explicit values win, and the single-purpose ones (as the generator does). </summary>
    private readonly record struct Attributes( bool Ignore, bool Marked, string? Name, bool ExtensionData, JsonReadOnlyMembers? ReadOnly )
    {
        public static Attributes Read( MemberInfo member )
        {
            bool?                memberIgnore = null, memberExtension = null;
            bool                 skip         = false, marked = false, overflow = false;
            string?              name         = null, key = null;
            JsonReadOnlyMembers? readOnly     = null;

            foreach ( CustomAttributeData attribute in member.GetCustomAttributesData() )
            {
                if ( attribute.AttributeType == typeof(JsonMemberAttribute) )
                {
                    marked = true;

                    foreach ( CustomAttributeNamedArgument argument in attribute.NamedArguments )
                    {
                        switch ( argument.MemberName )
                        {
                            case "Name":
                                name = argument.TypedValue.Value as string;
                                break;

                            case "Ignore":
                                memberIgnore = argument.TypedValue.Value is true;
                                break;

                            case "ExtensionData":
                                memberExtension = argument.TypedValue.Value is true;
                                break;

                            case "ReadOnly" when argument.TypedValue.Value is { } value && Convert.ToInt32(value) != 0:
                                readOnly = (JsonReadOnlyMembers)Convert.ToInt32(value);
                                break;
                        }
                    }

                    continue;
                }

                switch ( attribute.AttributeType.FullName )
                {
                    case J + "JsonKeyAttribute" when attribute.ConstructorArguments.Count == 1:
                        marked = true;
                        key    = attribute.ConstructorArguments[0].Value as string;
                        break;

                    case J + "JsonOverflowAttribute":
                        marked   = true;
                        overflow = true;
                        break;

                    case J + "JsonOptInAttribute":
                        marked = true;
                        break;

                    case J + "JsonSkipAttribute":
                        // JsonSkipCondition: Always = 0 (the default), WhenNull = 1, WhenDefault = 2; only Always leaves the member out of the schema.
                        skip = attribute.NamedArguments.FirstOrDefault(static a => a.MemberName == "Condition").TypedValue.Value is not { } c || Convert.ToInt32(c) == 0;
                        break;
                }
            }

            return new Attributes(memberIgnore ?? skip, marked, name ?? key, memberExtension ?? overflow, readOnly);
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
