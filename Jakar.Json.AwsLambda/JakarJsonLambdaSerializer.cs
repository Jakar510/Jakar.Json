// Jakar.Json.AwsLambda
// 10/17/2026

using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;



namespace Jakar.Json.AwsLambda;


/// <summary>
///     An AWS Lambda serializer on Jakar.Json: requests and responses of <c>[JsonModel]</c> types (and arrays, lists and collection interfaces of them) are read and written by Jakar.Json;
///     every other type (API Gateway, SQS and other event types) goes to <see cref="Fallback"/>.
/// </summary>
/// <remarks> For <c>[assembly: LambdaSerializer(typeof(...))]</c>, which needs a parameterless constructor, use <see cref="JakarJsonLambdaSerializer{TFallback}"/>. </remarks>
public class JakarJsonLambdaSerializer : ILambdaSerializer
{
    /// <summary> Jakar.Json, with System.Text.Json's <see cref="DefaultLambdaJsonSerializer"/> for other types. Under Native AOT, use <see cref="JakarJsonLambdaSerializer{TFallback}"/> with a source-generated fallback. </summary>
    [RequiresUnreferencedCode("DefaultLambdaJsonSerializer uses reflection; under Native AOT, use JakarJsonLambdaSerializer<SourceGeneratorLambdaJsonSerializer<TContext>>")]
    [RequiresDynamicCode("DefaultLambdaJsonSerializer uses reflection; under Native AOT, use JakarJsonLambdaSerializer<SourceGeneratorLambdaJsonSerializer<TContext>>")]
    public JakarJsonLambdaSerializer() : this(new DefaultLambdaJsonSerializer()) { }

    /// <param name="fallback"> Serializes the types Jakar.Json can't. </param>
    public JakarJsonLambdaSerializer( ILambdaSerializer fallback ) => Fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));


    public ILambdaSerializer Fallback { get; }


    public T Deserialize<T>( Stream requestStream )
    {
        if ( !JsonModels<T>.IsSupported ) { return Fallback.Deserialize<T>(requestStream); }

        using PooledBufferWriter buffer = new PooledBufferWriter().ReadFrom(requestStream);

        return (T)JsonModels<T>.Read(buffer.WrittenSpan)!;
    }

    public void Serialize<T>( T response, Stream responseStream )
    {
        if ( !JsonModels<T>.IsSupported )
        {
            Fallback.Serialize(response, responseStream);
            return;
        }

        using PooledBufferWriter buffer = new();
        JsonModels<T>.Write(response, buffer);
        responseStream.Write(buffer.WrittenSpan);
    }
}



/// <summary>
///     <see cref="JakarJsonLambdaSerializer"/> with a parameterless constructor, for <c>[assembly: LambdaSerializer(typeof(JakarJsonLambdaSerializer&lt;SourceGeneratorLambdaJsonSerializer&lt;MyContext&gt;&gt;))]</c>.
///     The fallback (here the source-generated System.Text.Json serializer) handles AWS event types, so the function stays Native AOT compatible.
/// </summary>
public sealed class JakarJsonLambdaSerializer<TFallback>() : JakarJsonLambdaSerializer(new TFallback())
    where TFallback : ILambdaSerializer, new();
