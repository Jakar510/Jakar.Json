// Jakar.Json.OpenApi
// 10/17/2026

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;



namespace Jakar.Json.OpenApi;


/// <summary>
///     Microsoft.AspNetCore.OpenApi: renames and filters the properties of <c>[JsonModel]</c> schemas to the names Jakar.Json writes (System.Text.Json's names come from its own options,
///     which can differ). Register with <c>services.AddOpenApi(o => o.AddJakarJsonSchemas())</c>.
/// </summary>
public sealed class JakarJsonOpenApiSchemaTransformer : IOpenApiSchemaTransformer
{
    public async Task TransformAsync( OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken )
    {
        JsonTypeInfo info = context.JsonTypeInfo;
        if ( info.Kind != JsonTypeInfoKind.Object || JsonModelContract.GetMembers(info.Type) is not { } members ) { return; }

        IDictionary<string, IOpenApiSchema>? properties = schema.Properties;
        Dictionary<string, IOpenApiSchema>   renamed    = new(StringComparer.Ordinal);
        HashSet<string>?                     required   = schema.Required is null ? null : new HashSet<string>(StringComparer.Ordinal);

        foreach ( JsonModelMember member in members )
        {
            JsonPropertyInfo? property = info.Properties.FirstOrDefault(p => Matches(p.AttributeProvider, member.Member));

            IOpenApiSchema? propertySchema = property is not null && properties?.TryGetValue(property.Name, out IOpenApiSchema? existing) is true
                                                 ? existing
                                                 : await context.GetOrCreateSchemaAsync(member.MemberType, cancellationToken: cancellationToken).ConfigureAwait(false);

            renamed[member.Name] = propertySchema;
            if ( property is not null && schema.Required?.Contains(property.Name) is true ) { required?.Add(member.Name); }
        }

        schema.Properties = renamed;
        if ( required is not null ) { schema.Required = required; }
    }

    private static bool Matches( ICustomAttributeProvider? provider, MemberInfo member ) => provider is MemberInfo info && info.Name == member.Name && info.MetadataToken == member.MetadataToken && info.Module == member.Module;
}



/// <summary>
///     Swashbuckle: describes <c>[JsonModel]</c> types with the property names Jakar.Json writes, and System.Text.Json's description for every other type. Register with
///     <c>services.AddSwaggerGen().AddJakarJsonSwagger()</c>.
/// </summary>
public sealed class JakarJsonDataContractResolver( ISerializerDataContractResolver inner ) : ISerializerDataContractResolver
{
    public DataContract GetDataContractForType( Type type )
    {
        DataContract contract = inner.GetDataContractForType(type);
        if ( contract.DataType != DataType.Object || JsonModelContract.GetMembers(type) is not { } members ) { return contract; }

        Dictionary<MemberInfo, DataProperty> described = ( contract.ObjectProperties ?? [] ).Where(static p => p.MemberInfo is not null).GroupBy(static p => p.MemberInfo, MemberComparer.Instance).ToDictionary(static g => g.Key, static g => g.First(), MemberComparer.Instance);
        List<DataProperty>                   renamed   = [];

        foreach ( JsonModelMember member in members )
        {
            renamed.Add(described.TryGetValue(member.Member, out DataProperty? property)
                            ? new DataProperty(member.Name, property.MemberType, property.IsRequired, property.IsNullable, property.IsReadOnly, property.IsWriteOnly, property.MemberInfo)
                            : new DataProperty(member.Name, member.MemberType, isReadOnly: !member.IsRead, isWriteOnly: !member.IsWritten, memberInfo: member.Member));
        }

        return DataContract.ForObject(contract.UnderlyingType, renamed, contract.ObjectExtensionDataType, contract.ObjectTypeNameProperty, contract.ObjectTypeNameValue, contract.JsonConverter);
    }



    private sealed class MemberComparer : IEqualityComparer<MemberInfo>
    {
        public static readonly MemberComparer Instance = new();

        public bool Equals( MemberInfo? x, MemberInfo? y ) => ReferenceEquals(x, y) || x is not null && y is not null && x.MetadataToken == y.MetadataToken && x.Module == y.Module;
        public int  GetHashCode( MemberInfo obj ) => HashCode.Combine(obj.MetadataToken, obj.Module);
    }
}



public static class JakarJsonOpenApiExtensions
{
    /// <summary> Microsoft.AspNetCore.OpenApi: gives <c>[JsonModel]</c> schemas the property names Jakar.Json writes. </summary>
    public static OpenApiOptions AddJakarJsonSchemas( this OpenApiOptions options )
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.AddSchemaTransformer(new JakarJsonOpenApiSchemaTransformer());
    }

    /// <summary>
    ///     Swashbuckle: gives <c>[JsonModel]</c> schemas the property names Jakar.Json writes. Other types are described from System.Text.Json's options, picked as Swashbuckle does (MVC's
    ///     JSON options, then minimal APIs').
    /// </summary>
    public static IServiceCollection AddJakarJsonSwagger( this IServiceCollection services )
    {
        ArgumentNullException.ThrowIfNull(services);
        services.RemoveAll<ISerializerDataContractResolver>();
        services.AddTransient<ISerializerDataContractResolver>(static provider => new JakarJsonDataContractResolver(new JsonSerializerDataContractResolver(GetJsonSerializerOptions(provider))));
        return services;
    }


    private static JsonSerializerOptions GetJsonSerializerOptions( IServiceProvider provider ) => provider.GetService<IOptions<Microsoft.AspNetCore.Mvc.JsonOptions>>()?.Value.JsonSerializerOptions ?? provider.GetService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()?.Value.SerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
}
