// Jakar.Json.Marten
// 10/17/2026

using Jakar.Json.SystemTextJson;
using Marten;
using Weasel.Core;



namespace Jakar.Json.Marten;


public static class JakarJsonMartenExtensions
{
    /// <summary>
    ///     Uses Marten's System.Text.Json serializer with <c>[JsonModel]</c> documents and events (and arrays, lists and collection interfaces of them) written and read by Jakar.Json.
    /// </summary>
    /// <remarks>
    ///     Marten's LINQ queries name JSON paths from <paramref name="casing"/>, not from the documents. Use the casing your models are written with: <see cref="Casing.CamelCase"/> for
    ///     <c>[JsonModel(Naming = JsonNaming.CamelCase)]</c>, <see cref="Casing.Default"/> for names as declared, <see cref="Casing.SnakeCase"/> for snake_case.
    /// </remarks>
    public static StoreOptions UseJakarJson( this StoreOptions options, Casing casing = Casing.CamelCase, EnumStorage enumStorage = EnumStorage.AsInteger )
    {
        ArgumentNullException.ThrowIfNull(options);
        options.UseSystemTextJsonForSerialization(enumStorage, casing, static json => json.AddJakarJson());
        return options;
    }
}
