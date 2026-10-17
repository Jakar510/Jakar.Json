// Jakar.Json.Http
// 10/17/2026

using System.Net.Http;
using System.Text;



namespace Jakar.Json.Http;


/// <summary> Reads <c>[JsonModel]</c> values from HTTP content with Jakar.Json. </summary>
public static class JakarJsonHttpContentExtensions
{
    /// <summary>
    ///     Reads the body as <typeparamref name="T"/>. UTF-8 bodies (no charset, or <c>utf-8</c>) are parsed as they are; a body in another charset is transcoded first. A JSON <c>null</c>
    ///     body is <see langword="default"/>.
    /// </summary>
    /// <exception cref="NotSupportedException"> Jakar.Json can't serialize <typeparamref name="T"/>. </exception>
    /// <exception cref="JsonReadException"> The body isn't valid JSON for <typeparamref name="T"/>. </exception>
    public static async Task<T?> ReadFromJakarJsonAsync<T>( this HttpContent content, CancellationToken token = default )
    {
        ArgumentNullException.ThrowIfNull(content);
        JsonTypeCodec codec = JsonModels<T>.RequiredCodec;

        if ( GetEncoding(content) is not null )
        {
            string text = await content.ReadAsStringAsync(token).ConfigureAwait(false);
            return (T?)codec.Read(Encoding.UTF8.GetBytes(text));
        }

        Stream stream = await content.ReadAsStreamAsync(token).ConfigureAwait(false);
        await using ( stream.ConfigureAwait(false) ) { return (T?)await codec.ReadAsync(stream, token: token).ConfigureAwait(false); }
    }


    /// <summary> The body's charset when it isn't UTF-8 (or ASCII, a subset); <see langword="null"/> for UTF-8 or none. </summary>
    private static Encoding? GetEncoding( HttpContent content )
    {
        string? charset = content.Headers.ContentType?.CharSet?.Trim('"');
        if ( string.IsNullOrEmpty(charset) ) { return null; }

        Encoding encoding;
        try { encoding = Encoding.GetEncoding(charset); }
        catch ( ArgumentException e ) { throw new InvalidOperationException($"The response's charset '{charset}' isn't supported", e); }

        return encoding.CodePage is 65001 or 20127
                   ? null
                   : encoding;
    }
}



/// <summary> Sends and receives <c>[JsonModel]</c> values with Jakar.Json: the <c>System.Net.Http.Json</c> methods, named <c>…JakarJson…</c>. </summary>
public static class JakarJsonHttpClientExtensions
{
    /// <summary> Sends a GET request and reads the successful response's body as <typeparamref name="T"/>. </summary>
    /// <exception cref="HttpRequestException"> The response status isn't a success. </exception>
    public static Task<T?> GetFromJakarJsonAsync<T>( this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken token = default ) => client.GetFromJakarJsonAsync<T>(CreateUri(requestUri), token);

    /// <inheritdoc cref="GetFromJakarJsonAsync{T}(HttpClient, string?, CancellationToken)"/>
    public static async Task<T?> GetFromJakarJsonAsync<T>( this HttpClient client, Uri? requestUri, CancellationToken token = default )
    {
        ArgumentNullException.ThrowIfNull(client);
        using HttpResponseMessage response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJakarJsonAsync<T>(token).ConfigureAwait(false);
    }

    /// <summary> Sends a DELETE request and reads the successful response's body as <typeparamref name="T"/>. </summary>
    /// <exception cref="HttpRequestException"> The response status isn't a success. </exception>
    public static Task<T?> DeleteFromJakarJsonAsync<T>( this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, CancellationToken token = default ) => client.DeleteFromJakarJsonAsync<T>(CreateUri(requestUri), token);

    /// <inheritdoc cref="DeleteFromJakarJsonAsync{T}(HttpClient, string?, CancellationToken)"/>
    public static async Task<T?> DeleteFromJakarJsonAsync<T>( this HttpClient client, Uri? requestUri, CancellationToken token = default )
    {
        ArgumentNullException.ThrowIfNull(client);
        using HttpResponseMessage response = await client.DeleteAsync(requestUri, token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJakarJsonAsync<T>(token).ConfigureAwait(false);
    }

    /// <summary> Sends <paramref name="value"/> as a Jakar.Json body with POST. The response is returned as is (check its status). </summary>
    public static Task<HttpResponseMessage> PostAsJakarJsonAsync<T>( this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, T value, CancellationToken token = default ) => client.PostAsJakarJsonAsync(CreateUri(requestUri), value, token);

    /// <inheritdoc cref="PostAsJakarJsonAsync{T}(HttpClient, string?, T, CancellationToken)"/>
    public static Task<HttpResponseMessage> PostAsJakarJsonAsync<T>( this HttpClient client, Uri? requestUri, T value, CancellationToken token = default )
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.PostAsync(requestUri, JakarJsonContent.Create(value), token);
    }

    /// <summary> Sends <paramref name="value"/> as a Jakar.Json body with PUT. The response is returned as is (check its status). </summary>
    public static Task<HttpResponseMessage> PutAsJakarJsonAsync<T>( this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, T value, CancellationToken token = default ) => client.PutAsJakarJsonAsync(CreateUri(requestUri), value, token);

    /// <inheritdoc cref="PutAsJakarJsonAsync{T}(HttpClient, string?, T, CancellationToken)"/>
    public static Task<HttpResponseMessage> PutAsJakarJsonAsync<T>( this HttpClient client, Uri? requestUri, T value, CancellationToken token = default )
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.PutAsync(requestUri, JakarJsonContent.Create(value), token);
    }

    /// <summary> Sends <paramref name="value"/> as a Jakar.Json body with PATCH. The response is returned as is (check its status). </summary>
    public static Task<HttpResponseMessage> PatchAsJakarJsonAsync<T>( this HttpClient client, [StringSyntax(StringSyntaxAttribute.Uri)] string? requestUri, T value, CancellationToken token = default ) => client.PatchAsJakarJsonAsync(CreateUri(requestUri), value, token);

    /// <inheritdoc cref="PatchAsJakarJsonAsync{T}(HttpClient, string?, T, CancellationToken)"/>
    public static Task<HttpResponseMessage> PatchAsJakarJsonAsync<T>( this HttpClient client, Uri? requestUri, T value, CancellationToken token = default )
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.PatchAsync(requestUri, JakarJsonContent.Create(value), token);
    }


    private static Uri? CreateUri( string? uri ) => string.IsNullOrEmpty(uri)
                                                       ? null
                                                       : new Uri(uri, UriKind.RelativeOrAbsolute);
}
