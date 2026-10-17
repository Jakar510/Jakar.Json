// Jakar.Json.Cap
// 10/17/2026

using DotNetCore.CAP;
using Jakar.Json.SystemTextJson;



namespace Jakar.Json.Cap;


public static class JakarJsonCapExtensions
{
    /// <summary>
    ///     Writes and reads message payloads of <c>[JsonModel]</c> types (and arrays, lists and collection interfaces of them) with Jakar.Json, through CAP's System.Text.Json serializer:
    ///     <c>services.AddCap(x =&gt; { x.UseJakarJson(); ... })</c>. CAP's headers and every other type keep System.Text.Json.
    /// </summary>
    public static CapOptions UseJakarJson( this CapOptions options )
    {
        ArgumentNullException.ThrowIfNull(options);
        options.JsonSerializerOptions.AddJakarJson();
        return options;
    }
}
