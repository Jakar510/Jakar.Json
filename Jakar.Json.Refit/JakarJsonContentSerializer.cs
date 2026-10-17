// Jakar.Json.Refit
// 10/17/2026

using System.Net.Http;
using System.Reflection;
using Jakar.Json.Http;
using Refit;



namespace Jakar.Json.Refit;


/// <summary>
///     Refit's content serializer on Jakar.Json: <c>RestService.For&lt;IOrdersApi&gt;(client, new RefitSettings().UseJakarJson())</c>. Bodies of <c>[JsonModel]</c> types (and arrays, lists
///     and collection interfaces of them) are written and read by Jakar.Json; every other type goes to <see cref="Fallback"/>.
/// </summary>
/// <param name="fallback"> Serializes the types Jakar.Json can't. Defaults to Refit's <see cref="SystemTextJsonContentSerializer"/>. </param>
public sealed class JakarJsonContentSerializer( IHttpContentSerializer? fallback = null ) : IHttpContentSerializer
{
    /// <summary> Handles every type Jakar.Json can't, and names properties for query strings and forms. </summary>
    public IHttpContentSerializer Fallback { get; } = fallback ?? new SystemTextJsonContentSerializer();


    public HttpContent ToHttpContent<T>( T item ) => JsonModels<T>.IsSupported
                                                         ? JakarJsonContent.Create(item)
                                                         : Fallback.ToHttpContent(item);

    public Task<T?> FromHttpContentAsync<T>( HttpContent content, CancellationToken cancellationToken = default ) => JsonModels<T>.IsSupported
                                                                                                                       ? content.ReadFromJakarJsonAsync<T>(cancellationToken)
                                                                                                                       : Fallback.FromHttpContentAsync<T>(content, cancellationToken);

    /// <summary> Delegated to <see cref="Fallback"/>: Refit asks it for query-string and form field names. </summary>
    public string? GetFieldNameForProperty( PropertyInfo propertyInfo ) => Fallback.GetFieldNameForProperty(propertyInfo);
}



public static class JakarJsonRefitSettingsExtensions
{
    /// <summary> Uses <see cref="JakarJsonContentSerializer"/>, with the current content serializer as its fallback. Returns <paramref name="settings"/>. </summary>
    public static RefitSettings UseJakarJson( this RefitSettings settings )
    {
        ArgumentNullException.ThrowIfNull(settings);
        if ( settings.ContentSerializer is not JakarJsonContentSerializer ) { settings.ContentSerializer = new JakarJsonContentSerializer(settings.ContentSerializer); }

        return settings;
    }
}
