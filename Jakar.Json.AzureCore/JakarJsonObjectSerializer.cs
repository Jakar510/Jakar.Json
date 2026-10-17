// Jakar.Json.AzureCore
// 10/17/2026

using System.Reflection;
using Azure.Core.Serialization;



namespace Jakar.Json.AzureCore;


/// <summary>
///     An Azure SDK <see cref="ObjectSerializer"/> on Jakar.Json: <c>[JsonModel]</c> types (and arrays, lists and collection interfaces of them) are read and written by Jakar.Json; every other
///     type goes to <see cref="Fallback"/>. Member names for queries and filters (<see cref="IMemberNameConverter"/>) come from the fallback.
/// </summary>
public sealed class JakarJsonObjectSerializer : ObjectSerializer, IMemberNameConverter
{
    /// <summary> Jakar.Json, with <see cref="JsonObjectSerializer.Default"/> (reflection-based System.Text.Json) for other types. Under Native AOT, pass a source-generated fallback instead. </summary>
    [RequiresUnreferencedCode("JsonObjectSerializer.Default uses reflection-based System.Text.Json; pass a JsonObjectSerializer built on a JsonSerializerContext instead")]
    [RequiresDynamicCode("JsonObjectSerializer.Default uses reflection-based System.Text.Json; pass a JsonObjectSerializer built on a JsonSerializerContext instead")]
    public JakarJsonObjectSerializer() : this(JsonObjectSerializer.Default) { }

    /// <param name="fallback"> Serializes the types Jakar.Json can't, and names members. </param>
    public JakarJsonObjectSerializer( ObjectSerializer fallback ) => Fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));


    public ObjectSerializer Fallback { get; }


    public override void Serialize( Stream stream, object? value, Type inputType, CancellationToken cancellationToken )
    {
        ArgumentNullException.ThrowIfNull(stream);

        if ( !JsonCodecRegistry.TryGet(inputType, out JsonTypeCodec? codec) )
        {
            Fallback.Serialize(stream, value, inputType, cancellationToken);
            return;
        }

        using PooledBufferWriter buffer = new();
        codec.Write(value, buffer);
        stream.Write(buffer.WrittenSpan);
    }

    public override async ValueTask SerializeAsync( Stream stream, object? value, Type inputType, CancellationToken cancellationToken )
    {
        ArgumentNullException.ThrowIfNull(stream);

        if ( !JsonCodecRegistry.TryGet(inputType, out JsonTypeCodec? codec) )
        {
            await Fallback.SerializeAsync(stream, value, inputType, cancellationToken).ConfigureAwait(false);
            return;
        }

        using PooledBufferWriter buffer = new();
        codec.Write(value, buffer);
        await stream.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }

    public override BinaryData Serialize( object? value, Type? inputType = null, CancellationToken cancellationToken = default )
    {
        inputType ??= value?.GetType() ?? typeof(object);
        if ( !JsonCodecRegistry.TryGet(inputType, out JsonTypeCodec? codec) ) { return Fallback.Serialize(value, inputType, cancellationToken); }

        using PooledBufferWriter buffer = new();
        codec.Write(value, buffer);
        return new BinaryData(buffer.ToArray());
    }

    public override ValueTask<BinaryData> SerializeAsync( object? value, Type? inputType = null, CancellationToken cancellationToken = default ) => ValueTask.FromResult(Serialize(value, inputType, cancellationToken));

    public override object? Deserialize( Stream stream, Type returnType, CancellationToken cancellationToken )
    {
        ArgumentNullException.ThrowIfNull(stream);
        if ( !JsonCodecRegistry.TryGet(returnType, out JsonTypeCodec? codec) ) { return Fallback.Deserialize(stream, returnType, cancellationToken); }

        using PooledBufferWriter buffer = new PooledBufferWriter().ReadFrom(stream);

        return codec.Read(buffer.WrittenSpan);
    }

    public override async ValueTask<object?> DeserializeAsync( Stream stream, Type returnType, CancellationToken cancellationToken )
    {
        ArgumentNullException.ThrowIfNull(stream);

        return JsonCodecRegistry.TryGet(returnType, out JsonTypeCodec? codec)
                   ? await codec.ReadAsync(stream, token: cancellationToken).ConfigureAwait(false)
                   : await Fallback.DeserializeAsync(stream, returnType, cancellationToken).ConfigureAwait(false);
    }

    /// <summary> The fallback's name for <paramref name="member"/> (Azure Search builds filters and field lists from these), or <see langword="null"/>. </summary>
    public string? ConvertMemberName( MemberInfo member ) => ( Fallback as IMemberNameConverter )?.ConvertMemberName(member);
}
