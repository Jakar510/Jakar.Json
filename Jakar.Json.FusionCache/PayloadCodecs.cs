// Jakar.Json.FusionCache
// 10/17/2026

namespace Jakar.Json.FusionCache;


/// <summary> Reads and writes one payload type: a bare value, or a <see cref="FusionCacheDistributedEntry{TValue}"/>. </summary>
internal abstract class PayloadCodec<T>
{
    public abstract byte[] Serialize( T?                  value );
    public abstract T?     Deserialize( ReadOnlySpan<byte> data );
}



/// <summary> The codec for <typeparamref name="T"/>, resolved once per type; <see langword="null"/> when the fallback serializer has to handle it. </summary>
internal static class PayloadCodecs<T>
{
    public static readonly PayloadCodec<T>? Codec = Resolve();


    private static PayloadCodec<T>? Resolve()
    {
        Type type = typeof(T);

        if ( type.IsGenericType && type.GetGenericTypeDefinition() == typeof(FusionCacheDistributedEntry<>) ) { return JakarJsonFusionCache.GetEntryCodec(type) as PayloadCodec<T>; }

        return ValueCodec<T>.IsSupported
                   ? new ValueOnlyCodec<T>()
                   : null;
    }
}



/// <summary> A value serialized on its own (not inside a FusionCache entry): the header, then the value. </summary>
internal sealed class ValueOnlyCodec<T> : PayloadCodec<T>
{
    public override byte[] Serialize( T? value )
    {
        ArrayBufferWriter<byte> buffer = Envelope.Rent();
        Envelope.WriteHeader(buffer, Envelope.SHAPE_VALUE);
        ValueCodec<T>.WriteSection(buffer, value);
        return Envelope.ToArray(buffer);
    }

    public override T? Deserialize( ReadOnlySpan<byte> data )
    {
        EnvelopeReader reader = new(data, Envelope.SHAPE_VALUE);
        return ValueCodec<T>.ReadSection(reader.Remaining);
    }
}



/// <summary>
///     A <see cref="FusionCacheDistributedEntry{TValue}"/>: the header, <see cref="FusionCacheDistributedEntry{TValue}.Timestamp"/>,
///     <see cref="FusionCacheDistributedEntry{TValue}.LogicalExpirationTimestamp"/>, the metadata, the tags, then the value.
/// </summary>
internal sealed class EntryCodec<TValue> : PayloadCodec<FusionCacheDistributedEntry<TValue>>
{
    // Which metadata fields follow; METADATA means there's a metadata object at all.
    private const byte METADATA      = 1 << 0;
    private const byte STALE         = 1 << 1;
    private const byte EAGER         = 1 << 2;
    private const byte ETAG          = 1 << 3;
    private const byte LAST_MODIFIED = 1 << 4;
    private const byte SIZE          = 1 << 5;
    private const byte PRIORITY      = 1 << 6;


    public override byte[] Serialize( FusionCacheDistributedEntry<TValue>? entry )
    {
        ArgumentNullException.ThrowIfNull(entry);

        ArrayBufferWriter<byte> buffer = Envelope.Rent();
        Envelope.WriteHeader(buffer, Envelope.SHAPE_ENTRY);
        Envelope.WriteInt64(buffer, entry.Timestamp);
        Envelope.WriteInt64(buffer, entry.LogicalExpirationTimestamp);
        WriteMetadata(buffer, entry.Metadata);
        WriteTags(buffer, entry.Tags);
        ValueCodec<TValue>.WriteSection(buffer, entry.Value);
        return Envelope.ToArray(buffer);
    }

    public override FusionCacheDistributedEntry<TValue> Deserialize( ReadOnlySpan<byte> data )
    {
        EnvelopeReader            reader                     = new(data, Envelope.SHAPE_ENTRY);
        long                      timestamp                  = reader.ReadInt64();
        long                      logicalExpirationTimestamp = reader.ReadInt64();
        FusionCacheEntryMetadata? metadata                   = ReadMetadata(ref reader);
        string[]?                 tags                       = ReadTags(ref reader);
        TValue?                   value                      = ValueCodec<TValue>.ReadSection(reader.Remaining);

        return new FusionCacheDistributedEntry<TValue>(value!, timestamp, logicalExpirationTimestamp, tags!, metadata!);
    }


    private static void WriteMetadata( ArrayBufferWriter<byte> buffer, FusionCacheEntryMetadata? metadata )
    {
        if ( metadata is null )
        {
            Envelope.WriteByte(buffer, 0);
            return;
        }

        byte flags = METADATA;
        if ( metadata.IsStale ) { flags                          |= STALE; }
        if ( metadata.EagerExpirationTimestamp.HasValue ) { flags |= EAGER; }
        if ( metadata.ETag is not null ) { flags                  |= ETAG; }
        if ( metadata.LastModifiedTimestamp.HasValue ) { flags    |= LAST_MODIFIED; }
        if ( metadata.Size.HasValue ) { flags                     |= SIZE; }
        if ( metadata.Priority.HasValue ) { flags                 |= PRIORITY; }

        Envelope.WriteByte(buffer, flags);
        if ( metadata.EagerExpirationTimestamp is { } eager ) { Envelope.WriteInt64(buffer, eager); }
        if ( metadata.ETag is { } etag ) { Envelope.WriteString(buffer, etag); }
        if ( metadata.LastModifiedTimestamp is { } lastModified ) { Envelope.WriteInt64(buffer, lastModified); }
        if ( metadata.Size is { } size ) { Envelope.WriteInt64(buffer, size); }
        if ( metadata.Priority is { } priority ) { Envelope.WriteByte(buffer, priority); }
    }

    private static FusionCacheEntryMetadata? ReadMetadata( ref EnvelopeReader reader )
    {
        byte flags = reader.ReadByte();
        if ( ( flags & METADATA ) == 0 ) { return null; }

        long?   eager        = ( flags & EAGER )         != 0 ? reader.ReadInt64() : null;
        string? etag         = ( flags & ETAG )          != 0 ? reader.ReadString() : null;
        long?   lastModified = ( flags & LAST_MODIFIED ) != 0 ? reader.ReadInt64() : null;
        long?   size         = ( flags & SIZE )          != 0 ? reader.ReadInt64() : null;
        byte?   priority     = ( flags & PRIORITY )      != 0 ? reader.ReadByte() : null;

        return new FusionCacheEntryMetadata(( flags & STALE ) != 0, eager, etag!, lastModified, size, priority);
    }

    private static void WriteTags( ArrayBufferWriter<byte> buffer, string[]? tags )
    {
        if ( tags is null )
        {
            Envelope.WriteInt32(buffer, -1);
            return;
        }

        Envelope.WriteInt32(buffer, tags.Length);
        foreach ( string tag in tags ) { Envelope.WriteString(buffer, tag); }
    }

    private static string[]? ReadTags( ref EnvelopeReader reader )
    {
        int count = reader.ReadInt32();
        if ( count < 0 ) { return null; }

        if ( count == 0 ) { return []; }

        if ( count > reader.Remaining.Length / sizeof(int) ) { throw Envelope.Corrupt("tag count exceeds the payload"); } // each tag needs at least its length

        string[] tags = new string[count];
        for ( int i = 0; i < tags.Length; i++ ) { tags[i] = reader.ReadString() ?? throw Envelope.Corrupt("null tag"); }

        return tags;
    }
}
