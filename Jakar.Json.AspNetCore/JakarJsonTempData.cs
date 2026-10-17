// Jakar.Json.AspNetCore
// 10/17/2026

using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;



namespace Jakar.Json.AspNetCore;


/// <summary>
///     TempData that can hold <c>[JsonModel]</c> values (and arrays, lists and collection interfaces of them), which MVC's default serializer rejects: <c>AddControllersWithViews().AddJakarJsonTempData()</c>.
///     A model is stored as a marked Jakar.Json string recording its type, and read back as that type; every other value is left to the serializer MVC registered.
/// </summary>
/// <remarks> TempData is data-protected (cookies) or server-side (session), so the stored type names can't be tampered with; only types <see cref="JsonCodecRegistry"/> resolves are read. </remarks>
/// <param name="inner"> The serializer MVC registered, for every other value and for the encoding itself. </param>
public sealed class JakarJsonTempDataSerializer( TempDataSerializer inner ) : TempDataSerializer
{
    // A prefix no ordinary TempData string starts with: SOH, the marker, SOH, then "{type}\n{json}".
    private const string MARKER = "\u0001jakar-json\u0001";


    public TempDataSerializer Inner { get; } = inner ?? throw new ArgumentNullException(nameof(inner));


    public override bool CanSerializeType( Type type ) => JsonCodecRegistry.IsRegistered(type) || Inner.CanSerializeType(type);

    public override byte[] Serialize( IDictionary<string, object> values )
    {
        if ( values is null || !values.Values.Any(static v => v is not null && JsonCodecRegistry.IsRegistered(v.GetType())) ) { return Inner.Serialize(values!); }

        Dictionary<string, object> converted = new(values.Count, StringComparer.OrdinalIgnoreCase);

        foreach ( ( string key, object value ) in values )
        {
            converted[key] = value is not null && JsonCodecRegistry.TryGet(value.GetType(), out JsonTypeCodec? codec)
                                 ? Encode(value, codec)
                                 : value!;
        }

        return Inner.Serialize(converted);
    }

    public override IDictionary<string, object> Deserialize( byte[] unprotectedData )
    {
        IDictionary<string, object> values = Inner.Deserialize(unprotectedData);

        foreach ( string key in values.Keys.ToArray() )
        {
            if ( values[key] is string text && text.StartsWith(MARKER, StringComparison.Ordinal) ) { values[key] = Decode(text)!; }
        }

        return values;
    }


    private static string Encode( object value, JsonTypeCodec codec )
    {
        ArrayBufferWriter<byte> buffer = new();
        codec.Write(value, buffer);
        return string.Concat(MARKER, value.GetType().AssemblyQualifiedName, "\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "The name was written by Encode for a type the app used; only types JsonCodecRegistry resolves are read, and TempData is data-protected")]
    private static object? Decode( string text )
    {
        int    newline  = text.IndexOf('\n', MARKER.Length);
        string typeName = text[MARKER.Length..newline];

        Type? type = Type.GetType(typeName, throwOnError: false);
        if ( type is null || !JsonCodecRegistry.TryGet(type, out JsonTypeCodec? codec) ) { throw new InvalidOperationException($"TempData holds a {typeName}, which Jakar.Json can't read here"); }

        return codec.Read(Encoding.UTF8.GetBytes(text, newline + 1, text.Length - newline - 1));
    }
}



public static class JakarJsonTempDataExtensions
{
    /// <summary> Lets TempData hold <c>[JsonModel]</c> values, around the TempData serializer MVC registered. Call it after <c>AddControllersWithViews()</c> / <c>AddRazorPages()</c>. </summary>
    public static IMvcBuilder AddJakarJsonTempData( this IMvcBuilder builder )
    {
        ArgumentNullException.ThrowIfNull(builder);
        Decorate(builder.Services);
        return builder;
    }


    private static void Decorate( IServiceCollection services )
    {
        ServiceDescriptor registered = services.LastOrDefault(static d => d.ServiceType == typeof(TempDataSerializer)) ?? throw new InvalidOperationException("No TempData serializer is registered: call AddControllersWithViews() or AddRazorPages() first");
        if ( registered.ImplementationFactory?.Target is TempDataDecoration ) { return; }

        services.Remove(registered);
        services.AddSingleton<TempDataSerializer>(new TempDataDecoration(registered).Create);
    }



    private sealed class TempDataDecoration( ServiceDescriptor registered )
    {
        public TempDataSerializer Create( IServiceProvider provider ) => new JakarJsonTempDataSerializer(registered.ImplementationInstance as TempDataSerializer ?? registered.ImplementationFactory?.Invoke(provider) as TempDataSerializer ?? (TempDataSerializer)ActivatorUtilities.CreateInstance(provider, registered.ImplementationType!));
    }
}
