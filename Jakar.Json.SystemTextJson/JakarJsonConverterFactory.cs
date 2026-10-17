// Jakar.Json.SystemTextJson
// 10/17/2026

using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;



namespace Jakar.Json.SystemTextJson;


/// <summary>
///     Routes every <c>[JsonModel]</c> type (and arrays, lists and collection interfaces of them) through Jakar.Json inside System.Text.Json: <c>options.AddJakarJson()</c>. Other types keep
///     System.Text.Json's own handling, so hosts configured through <see cref="JsonSerializerOptions"/> adopt Jakar.Json one model at a time.
/// </summary>
/// <remarks>
///     Native AOT: System.Text.Json needs a <see cref="JsonConverter{T}"/> per type, which this factory creates with <see cref="Type.MakeGenericType"/> when dynamic code is available.
///     Under Native AOT it converts only the types added with <see cref="Add{T}"/> (or use <see cref="JakarJsonConverter{T}"/> directly).
/// </remarks>
public sealed class JakarJsonConverterFactory : JsonConverterFactory
{
    private readonly ConcurrentDictionary<Type, JsonConverter> __added = new();


    /// <summary> Converts <typeparamref name="T"/> without dynamic code; needed only under Native AOT. Returns this factory, so calls chain. </summary>
    /// <exception cref="NotSupportedException"> Jakar.Json can't serialize <typeparamref name="T"/>. </exception>
    public JakarJsonConverterFactory Add<T>()
    {
        __added.TryAdd(typeof(T), new JakarJsonConverter<T>());
        return this;
    }


    public override bool CanConvert( Type typeToConvert ) => __added.ContainsKey(typeToConvert) || ( RuntimeFeature.IsDynamicCodeSupported && JsonModels.IsSupported(typeToConvert) );

    public override JsonConverter? CreateConverter( Type typeToConvert, JsonSerializerOptions options )
    {
        if ( __added.TryGetValue(typeToConvert, out JsonConverter? added) ) { return added; }

        if ( !RuntimeFeature.IsDynamicCodeSupported ) { return null; }

        return Create(typeToConvert);
    }


    [RequiresDynamicCode("Creates JakarJsonConverter<T> for a type known only at run time; under Native AOT, Add<T>() is used instead")]
    private static JsonConverter Create( Type type ) => (JsonConverter)typeof(JakarJsonConverterFactory).GetMethod(nameof(CreateCore), BindingFlags.NonPublic | BindingFlags.Static)!
                                                                                                        .MakeGenericMethod(type)
                                                                                                        .Invoke(null, null)!;

    private static JsonConverter CreateCore<T>() => new JakarJsonConverter<T>();
}



public static class JakarJsonSerializerOptionsExtensions
{
    /// <summary>
    ///     Adds <see cref="JakarJsonConverterFactory"/> ahead of the other converters, so <c>[JsonModel]</c> types are read and written by Jakar.Json. Returns <paramref name="options"/>.
    /// </summary>
    /// <param name="options"> Options that aren't read-only yet (System.Text.Json locks them on first use). </param>
    /// <param name="configure"> Adds types for Native AOT: <c>f => f.Add&lt;Order&gt;()</c>. </param>
    public static JsonSerializerOptions AddJakarJson( this JsonSerializerOptions options, Action<JakarJsonConverterFactory>? configure = null )
    {
        ArgumentNullException.ThrowIfNull(options);

        JakarJsonConverterFactory factory = options.Converters.OfType<JakarJsonConverterFactory>().FirstOrDefault() ?? new JakarJsonConverterFactory();
        configure?.Invoke(factory);
        if ( !options.Converters.Contains(factory) ) { options.Converters.Insert(0, factory); }

        return options;
    }
}
