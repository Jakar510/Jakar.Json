// Jakar.Json.FusionCache
// 10/17/2026

namespace Jakar.Json.FusionCache;


/// <summary>
///     The binary layout around a cached value (little-endian):
///     <code>
///     0xFF 'J' version shape            header; 0xFF can't start a JSON document or a UTF-8 string, so other serializers' payloads are told apart
///     [entry fields]                     shape 1 only: see EntryCodec
///     present (1 byte) value...          the value runs to the end of the payload
///     </code>
///     Strings (tags, ETag) are an <see cref="int"/> byte count (-1 for <see langword="null"/>) followed by UTF-8.
/// </summary>
internal static class Envelope
{
    public const byte MAGIC       = 0xFF;
    public const byte MARKER      = (byte)'J';
    public const byte VERSION     = 1;
    public const byte SHAPE_VALUE = 0;
    public const byte SHAPE_ENTRY = 1;
    public const int  HEADER_SIZE = 4;

    /// <summary> Larger buffers aren't kept for the next serialization on the thread, so one huge value doesn't pin its buffer for the thread's lifetime. </summary>
    private const int MAX_RETAINED_BYTES = 1024 * 1024;

    [ThreadStatic] private static ArrayBufferWriter<byte>? __buffer;


    public static bool IsEnvelope( ReadOnlySpan<byte> data ) => data.Length >= HEADER_SIZE && data[0] == MAGIC && data[1] == MARKER;


    /// <summary> The thread's scratch buffer, emptied. Every payload is copied out by <see cref="ToArray"/>, which FusionCache's <c>byte[]</c> API needs anyway. </summary>
    public static ArrayBufferWriter<byte> Rent()
    {
        ArrayBufferWriter<byte> buffer = __buffer ??= new ArrayBufferWriter<byte>(256);
        buffer.ResetWrittenCount();
        return buffer;
    }

    public static byte[] ToArray( ArrayBufferWriter<byte> buffer )
    {
        byte[] payload = buffer.WrittenSpan.ToArray();
        if ( buffer.Capacity > MAX_RETAINED_BYTES ) { __buffer = null; }

        return payload;
    }


    public static void WriteHeader( ArrayBufferWriter<byte> buffer, byte shape )
    {
        Span<byte> span = buffer.GetSpan(HEADER_SIZE);
        span[0] = MAGIC;
        span[1] = MARKER;
        span[2] = VERSION;
        span[3] = shape;
        buffer.Advance(HEADER_SIZE);
    }

    public static void WriteByte( ArrayBufferWriter<byte> buffer, byte value )
    {
        buffer.GetSpan(1)[0] = value;
        buffer.Advance(1);
    }

    public static void WriteInt32( ArrayBufferWriter<byte> buffer, int value )
    {
        BinaryPrimitives.WriteInt32LittleEndian(buffer.GetSpan(sizeof(int)), value);
        buffer.Advance(sizeof(int));
    }

    public static void WriteInt64( ArrayBufferWriter<byte> buffer, long value )
    {
        BinaryPrimitives.WriteInt64LittleEndian(buffer.GetSpan(sizeof(long)), value);
        buffer.Advance(sizeof(long));
    }

    /// <summary> Byte count, then UTF-8. </summary>
    public static void WriteString( ArrayBufferWriter<byte> buffer, string? value )
    {
        if ( value is null )
        {
            WriteInt32(buffer, -1);
            return;
        }

        Span<byte> span    = buffer.GetSpan(sizeof(int) + Encoding.UTF8.GetMaxByteCount(value.Length));
        int        written = Encoding.UTF8.GetBytes(value, span[sizeof(int)..]);
        BinaryPrimitives.WriteInt32LittleEndian(span, written);
        buffer.Advance(sizeof(int) + written);
    }

    /// <summary> UTF-8 to the end of the payload: the last thing written, so no length. </summary>
    public static void WriteUtf8( ArrayBufferWriter<byte> buffer, string value )
    {
        int written = Encoding.UTF8.GetBytes(value, buffer.GetSpan(Encoding.UTF8.GetMaxByteCount(value.Length)));
        buffer.Advance(written);
    }


    public static FusionCacheSerializationException Corrupt( string reason ) => new($"Corrupt {nameof(FusionCacheJakarJsonSerializer)} payload: {reason}");
}



/// <summary> Reads an <see cref="Envelope"/> front to back, failing with <see cref="FusionCacheSerializationException"/> on anything malformed. </summary>
internal ref struct EnvelopeReader
{
    private readonly ReadOnlySpan<byte> __data;
    private          int                __position;


    /// <summary> Checks the header (magic, version, and that it holds <paramref name="shape"/>) and starts after it. </summary>
    public EnvelopeReader( ReadOnlySpan<byte> data, byte shape )
    {
        if ( !Envelope.IsEnvelope(data) ) { throw Envelope.Corrupt("missing header"); }

        if ( data[2] != Envelope.VERSION ) { throw new FusionCacheSerializationException($"Unsupported {nameof(FusionCacheJakarJsonSerializer)} payload version {data[2]} (this version reads {Envelope.VERSION})"); }

        if ( data[3] != shape )
        {
            throw new FusionCacheSerializationException(shape == Envelope.SHAPE_ENTRY
                                                            ? "The payload is a bare value, not a FusionCache entry"
                                                            : "The payload is a FusionCache entry, not a bare value");
        }

        __data     = data;
        __position = Envelope.HEADER_SIZE;
    }


    public readonly ReadOnlySpan<byte> Remaining => __data[__position..];


    public byte ReadByte() => Take(1)[0];
    public int  ReadInt32() => BinaryPrimitives.ReadInt32LittleEndian(Take(sizeof(int)));
    public long ReadInt64() => BinaryPrimitives.ReadInt64LittleEndian(Take(sizeof(long)));

    public string? ReadString()
    {
        int length = ReadInt32();

        return length switch
               {
                   -1  => null,
                   < 0 => throw Envelope.Corrupt("negative string length"),
                   _   => Encoding.UTF8.GetString(Take(length))
               };
    }


    private ReadOnlySpan<byte> Take( int count )
    {
        if ( count > __data.Length - __position ) { throw Envelope.Corrupt("truncated"); }

        ReadOnlySpan<byte> span = __data.Slice(__position, count);
        __position += count;
        return span;
    }
}
