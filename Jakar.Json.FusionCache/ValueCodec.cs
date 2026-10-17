// Jakar.Json.FusionCache
// 10/17/2026

namespace Jakar.Json.FusionCache;


internal enum ValueKind : byte
{
    Unsupported,
    Json,
    String,
    Bytes,
    Boolean,
    Byte,
    SByte,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Single,
    Double,
    Decimal,
    Char,
    Guid,
    DateTime,
    DateTimeOffset,
    DateOnly,
    TimeOnly,
    TimeSpan,
    Enum
}



/// <summary>
///     Writes and reads a value of <typeparamref name="T"/>: a <c>[JsonModel]</c> type or collection of one through its <see cref="JsonTypeCodec"/>, everything else supported in binary.
///     <see cref="Kind"/> is fixed per <typeparamref name="T"/>, so the JIT drops every other branch, and <see cref="Unsafe.As{TFrom,TTo}(ref TFrom)"/> reads the scalars without boxing.
/// </summary>
/// <remarks> <see cref="Nullable{T}"/> scalars aren't supported (they need a fallback serializer); nullable reference values are. </remarks>
internal static class ValueCodec<T>
{
    private const byte ABSENT  = 0;
    private const byte PRESENT = 1;

    public static readonly ValueKind      Kind;
    private static readonly JsonTypeCodec? __json;


    static ValueCodec() => Kind = Resolve(out __json);


    public static bool IsSupported => Kind != ValueKind.Unsupported;


    /// <summary> The presence byte, then the value; <see langword="null"/> writes only the presence byte. </summary>
    public static void WriteSection( ArrayBufferWriter<byte> buffer, T? value )
    {
        if ( value is null )
        {
            Envelope.WriteByte(buffer, ABSENT);
            return;
        }

        Envelope.WriteByte(buffer, PRESENT);
        Write(buffer, value);
    }

    public static T? ReadSection( ReadOnlySpan<byte> data )
    {
        if ( data.IsEmpty ) { throw Envelope.Corrupt("missing value"); }

        return data[0] switch
               {
                   ABSENT  => default,
                   PRESENT => Read(data[1..]),
                   _       => throw Envelope.Corrupt("invalid value marker")
               };
    }


    private static ValueKind Resolve( out JsonTypeCodec? json )
    {
        json = null;
        Type type = typeof(T);

        if ( type == typeof(string) ) { return ValueKind.String; }
        if ( type == typeof(byte[]) ) { return ValueKind.Bytes; }
        if ( type == typeof(bool) ) { return ValueKind.Boolean; }
        if ( type == typeof(byte) ) { return ValueKind.Byte; }
        if ( type == typeof(sbyte) ) { return ValueKind.SByte; }
        if ( type == typeof(short) ) { return ValueKind.Int16; }
        if ( type == typeof(ushort) ) { return ValueKind.UInt16; }
        if ( type == typeof(int) ) { return ValueKind.Int32; }
        if ( type == typeof(uint) ) { return ValueKind.UInt32; }
        if ( type == typeof(long) ) { return ValueKind.Int64; }
        if ( type == typeof(ulong) ) { return ValueKind.UInt64; }
        if ( type == typeof(float) ) { return ValueKind.Single; }
        if ( type == typeof(double) ) { return ValueKind.Double; }
        if ( type == typeof(decimal) ) { return ValueKind.Decimal; }
        if ( type == typeof(char) ) { return ValueKind.Char; }
        if ( type == typeof(Guid) ) { return ValueKind.Guid; }
        if ( type == typeof(DateTime) ) { return ValueKind.DateTime; }
        if ( type == typeof(DateTimeOffset) ) { return ValueKind.DateTimeOffset; }
        if ( type == typeof(DateOnly) ) { return ValueKind.DateOnly; }
        if ( type == typeof(TimeOnly) ) { return ValueKind.TimeOnly; }
        if ( type == typeof(TimeSpan) ) { return ValueKind.TimeSpan; }
        if ( type.IsEnum ) { return ValueKind.Enum; }

        return JsonCodecRegistry.TryGet(type, out json)
                   ? ValueKind.Json
                   : ValueKind.Unsupported;
    }


    private static void Write( ArrayBufferWriter<byte> buffer, T value )
    {
        switch ( Kind )
        {
            case ValueKind.Json:
                __json!.Write(value, buffer);
                return;

            case ValueKind.String:
                Envelope.WriteUtf8(buffer, Unsafe.As<T, string>(ref value));
                return;

            case ValueKind.Bytes:
                buffer.Write(Unsafe.As<T, byte[]>(ref value));
                return;

            case ValueKind.Boolean:
                Envelope.WriteByte(buffer, Unsafe.As<T, bool>(ref value) ? (byte)1 : (byte)0);
                return;

            case ValueKind.Byte:
                Envelope.WriteByte(buffer, Unsafe.As<T, byte>(ref value));
                return;

            case ValueKind.SByte:
                Envelope.WriteByte(buffer, (byte)Unsafe.As<T, sbyte>(ref value));
                return;

            case ValueKind.Int16:
                BinaryPrimitives.WriteInt16LittleEndian(Fixed(buffer, sizeof(short)), Unsafe.As<T, short>(ref value));
                break;

            case ValueKind.UInt16:
                BinaryPrimitives.WriteUInt16LittleEndian(Fixed(buffer, sizeof(ushort)), Unsafe.As<T, ushort>(ref value));
                break;

            case ValueKind.Char:
                BinaryPrimitives.WriteUInt16LittleEndian(Fixed(buffer, sizeof(char)), Unsafe.As<T, char>(ref value));
                break;

            case ValueKind.Int32:
                BinaryPrimitives.WriteInt32LittleEndian(Fixed(buffer, sizeof(int)), Unsafe.As<T, int>(ref value));
                break;

            case ValueKind.UInt32:
                BinaryPrimitives.WriteUInt32LittleEndian(Fixed(buffer, sizeof(uint)), Unsafe.As<T, uint>(ref value));
                break;

            case ValueKind.Int64:
                BinaryPrimitives.WriteInt64LittleEndian(Fixed(buffer, sizeof(long)), Unsafe.As<T, long>(ref value));
                break;

            case ValueKind.UInt64:
                BinaryPrimitives.WriteUInt64LittleEndian(Fixed(buffer, sizeof(ulong)), Unsafe.As<T, ulong>(ref value));
                break;

            case ValueKind.Single:
                BinaryPrimitives.WriteSingleLittleEndian(Fixed(buffer, sizeof(float)), Unsafe.As<T, float>(ref value));
                break;

            case ValueKind.Double:
                BinaryPrimitives.WriteDoubleLittleEndian(Fixed(buffer, sizeof(double)), Unsafe.As<T, double>(ref value));
                break;

            case ValueKind.Decimal:
            {
                Span<int> bits = stackalloc int[4];
                decimal.GetBits(Unsafe.As<T, decimal>(ref value), bits);
                Span<byte> span = Fixed(buffer, sizeof(decimal));
                for ( int i = 0; i < bits.Length; i++ ) { BinaryPrimitives.WriteInt32LittleEndian(span[( i * sizeof(int) )..], bits[i]); }

                break;
            }

            case ValueKind.Guid:
                Unsafe.As<T, Guid>(ref value).TryWriteBytes(Fixed(buffer, 16));
                break;

            case ValueKind.DateTime: // ToBinary keeps the Kind
                BinaryPrimitives.WriteInt64LittleEndian(Fixed(buffer, sizeof(long)), Unsafe.As<T, DateTime>(ref value).ToBinary());
                break;

            case ValueKind.DateTimeOffset:
            {
                DateTimeOffset dateTime = Unsafe.As<T, DateTimeOffset>(ref value);
                Span<byte>     span     = Fixed(buffer, sizeof(long) + sizeof(short));
                BinaryPrimitives.WriteInt64LittleEndian(span, dateTime.Ticks);
                BinaryPrimitives.WriteInt16LittleEndian(span[sizeof(long)..], (short)dateTime.TotalOffsetMinutes);
                break;
            }

            case ValueKind.DateOnly:
                BinaryPrimitives.WriteInt32LittleEndian(Fixed(buffer, sizeof(int)), Unsafe.As<T, DateOnly>(ref value).DayNumber);
                break;

            case ValueKind.TimeOnly:
                BinaryPrimitives.WriteInt64LittleEndian(Fixed(buffer, sizeof(long)), Unsafe.As<T, TimeOnly>(ref value).Ticks);
                break;

            case ValueKind.TimeSpan:
                BinaryPrimitives.WriteInt64LittleEndian(Fixed(buffer, sizeof(long)), Unsafe.As<T, TimeSpan>(ref value).Ticks);
                break;

            case ValueKind.Enum: // the underlying integer, at its own size
                switch ( Unsafe.SizeOf<T>() )
                {
                    case 1:
                        Envelope.WriteByte(buffer, Unsafe.As<T, byte>(ref value));
                        return;

                    case 2:
                        BinaryPrimitives.WriteUInt16LittleEndian(Fixed(buffer, 2), Unsafe.As<T, ushort>(ref value));
                        break;

                    case 4:
                        BinaryPrimitives.WriteUInt32LittleEndian(Fixed(buffer, 4), Unsafe.As<T, uint>(ref value));
                        break;

                    default:
                        BinaryPrimitives.WriteUInt64LittleEndian(Fixed(buffer, 8), Unsafe.As<T, ulong>(ref value));
                        break;
                }

                break;

            default: throw new NotSupportedException($"{typeof(T)} isn't supported");
        }

        buffer.Advance(FixedSize);
    }

    private static T Read( ReadOnlySpan<byte> data )
    {
        switch ( Kind )
        {
            case ValueKind.Json:
            {
                object? value;
                try { value = __json!.Read(data); }
                catch ( JsonReadException e ) { throw new FusionCacheSerializationException($"Corrupt {nameof(FusionCacheJakarJsonSerializer)} payload: {e.Message}", e); }

                return value is T model
                           ? model
                           : throw Envelope.Corrupt($"expected a {typeof(T).Name}, found JSON null");
            }

            case ValueKind.String:
            {
                string text = Encoding.UTF8.GetString(data);
                return Unsafe.As<string, T>(ref text);
            }

            case ValueKind.Bytes:
            {
                byte[] bytes = data.ToArray();
                return Unsafe.As<byte[], T>(ref bytes);
            }
        }

        if ( data.Length != FixedSize ) { throw Envelope.Corrupt($"{typeof(T).Name} takes {FixedSize} bytes, found {data.Length}"); }

        switch ( Kind )
        {
            case ValueKind.Boolean:
            {
                bool value = data[0] != 0;
                return Unsafe.As<bool, T>(ref value);
            }

            case ValueKind.Byte:
            {
                byte value = data[0];
                return Unsafe.As<byte, T>(ref value);
            }

            case ValueKind.SByte:
            {
                sbyte value = (sbyte)data[0];
                return Unsafe.As<sbyte, T>(ref value);
            }

            case ValueKind.Int16:
            {
                short value = BinaryPrimitives.ReadInt16LittleEndian(data);
                return Unsafe.As<short, T>(ref value);
            }

            case ValueKind.UInt16:
            {
                ushort value = BinaryPrimitives.ReadUInt16LittleEndian(data);
                return Unsafe.As<ushort, T>(ref value);
            }

            case ValueKind.Char:
            {
                char value = (char)BinaryPrimitives.ReadUInt16LittleEndian(data);
                return Unsafe.As<char, T>(ref value);
            }

            case ValueKind.Int32:
            {
                int value = BinaryPrimitives.ReadInt32LittleEndian(data);
                return Unsafe.As<int, T>(ref value);
            }

            case ValueKind.UInt32:
            {
                uint value = BinaryPrimitives.ReadUInt32LittleEndian(data);
                return Unsafe.As<uint, T>(ref value);
            }

            case ValueKind.Int64:
            {
                long value = BinaryPrimitives.ReadInt64LittleEndian(data);
                return Unsafe.As<long, T>(ref value);
            }

            case ValueKind.UInt64:
            {
                ulong value = BinaryPrimitives.ReadUInt64LittleEndian(data);
                return Unsafe.As<ulong, T>(ref value);
            }

            case ValueKind.Single:
            {
                float value = BinaryPrimitives.ReadSingleLittleEndian(data);
                return Unsafe.As<float, T>(ref value);
            }

            case ValueKind.Double:
            {
                double value = BinaryPrimitives.ReadDoubleLittleEndian(data);
                return Unsafe.As<double, T>(ref value);
            }

            case ValueKind.Decimal:
            {
                Span<int> bits = stackalloc int[4];
                for ( int i = 0; i < bits.Length; i++ ) { bits[i] = BinaryPrimitives.ReadInt32LittleEndian(data[( i * sizeof(int) )..]); }

                decimal value;
                try { value = new decimal(bits); }
                catch ( ArgumentException e ) { throw new FusionCacheSerializationException($"Corrupt {nameof(FusionCacheJakarJsonSerializer)} payload: invalid decimal", e); }

                return Unsafe.As<decimal, T>(ref value);
            }

            case ValueKind.Guid:
            {
                Guid value = new(data);
                return Unsafe.As<Guid, T>(ref value);
            }

            case ValueKind.DateTime:
            {
                DateTime value = DateTime.FromBinary(BinaryPrimitives.ReadInt64LittleEndian(data));
                return Unsafe.As<DateTime, T>(ref value);
            }

            case ValueKind.DateTimeOffset:
            {
                DateTimeOffset value = new(BinaryPrimitives.ReadInt64LittleEndian(data), TimeSpan.FromMinutes(BinaryPrimitives.ReadInt16LittleEndian(data[sizeof(long)..])));
                return Unsafe.As<DateTimeOffset, T>(ref value);
            }

            case ValueKind.DateOnly:
            {
                DateOnly value = DateOnly.FromDayNumber(BinaryPrimitives.ReadInt32LittleEndian(data));
                return Unsafe.As<DateOnly, T>(ref value);
            }

            case ValueKind.TimeOnly:
            {
                TimeOnly value = new(BinaryPrimitives.ReadInt64LittleEndian(data));
                return Unsafe.As<TimeOnly, T>(ref value);
            }

            case ValueKind.TimeSpan:
            {
                TimeSpan value = new(BinaryPrimitives.ReadInt64LittleEndian(data));
                return Unsafe.As<TimeSpan, T>(ref value);
            }

            case ValueKind.Enum:
                switch ( Unsafe.SizeOf<T>() )
                {
                    case 1:
                    {
                        byte value = data[0];
                        return Unsafe.As<byte, T>(ref value);
                    }

                    case 2:
                    {
                        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(data);
                        return Unsafe.As<ushort, T>(ref value);
                    }

                    case 4:
                    {
                        uint value = BinaryPrimitives.ReadUInt32LittleEndian(data);
                        return Unsafe.As<uint, T>(ref value);
                    }

                    default:
                    {
                        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(data);
                        return Unsafe.As<ulong, T>(ref value);
                    }
                }

            default: throw new NotSupportedException($"{typeof(T)} isn't supported");
        }
    }


    /// <summary> Bytes a fixed-size scalar takes; 0 for strings, byte arrays and JSON (they run to the end of the payload). </summary>
    private static int FixedSize => Kind switch
                                    {
                                        ValueKind.Boolean or ValueKind.Byte or ValueKind.SByte                           => 1,
                                        ValueKind.Int16 or ValueKind.UInt16 or ValueKind.Char                            => 2,
                                        ValueKind.Int32 or ValueKind.UInt32 or ValueKind.Single or ValueKind.DateOnly     => 4,
                                        ValueKind.Int64 or ValueKind.UInt64 or ValueKind.Double or ValueKind.DateTime
                                            or ValueKind.TimeOnly or ValueKind.TimeSpan                                  => 8,
                                        ValueKind.DateTimeOffset                                                         => sizeof(long) + sizeof(short),
                                        ValueKind.Decimal or ValueKind.Guid                                              => 16,
                                        ValueKind.Enum                                                                   => Unsafe.SizeOf<T>(),
                                        _                                                                                => 0
                                    };

    private static Span<byte> Fixed( ArrayBufferWriter<byte> buffer, int size ) => buffer.GetSpan(size)[..size];
}
