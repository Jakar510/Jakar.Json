// Jakar.Json.EntityFrameworkCore
// 10/17/2026

using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;



namespace Jakar.Json.EntityFrameworkCore;


/// <summary> Stores a <c>[JsonModel]</c> value (or an array, list or collection interface of one) as Jakar.Json text, in a <c>json</c>/<c>jsonb</c>/<c>nvarchar</c>/<c>TEXT</c> column. </summary>
/// <exception cref="NotSupportedException"> Jakar.Json can't serialize <typeparamref name="T"/>. </exception>
public sealed class JakarJsonValueConverter<T> : ValueConverter<T, string>
{
    public JakarJsonValueConverter() : base(static value => JakarJsonText<T>.Write(value), static text => JakarJsonText<T>.Read(text)!) => _ = JsonModels<T>.RequiredCodec; // fail at model building, not at the first query
}



/// <summary>
///     Change tracking for JSON columns: two values are equal when Jakar.Json writes the same text for them, and a snapshot is a deep copy (written and read back). Without it EF Core compares
///     references, so changes made inside a tracked model would be missed.
/// </summary>
public sealed class JakarJsonValueComparer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] T>() : ValueComparer<T>(static ( left, right ) => JakarJsonText<T>.AreEqual(left, right), static value => JakarJsonText<T>.Hash(value), static value => JakarJsonText<T>.Copy(value)!);



public static class JakarJsonPropertyBuilderExtensions
{
    /// <summary> Stores the property as Jakar.Json text, with <see cref="JakarJsonValueComparer{T}"/> for change tracking. Set the column type for your provider (<c>HasColumnType("jsonb")</c>). </summary>
    public static PropertyBuilder<T> HasJakarJsonConversion<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] T>( this PropertyBuilder<T> builder )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.HasConversion(new JakarJsonValueConverter<T>(), new JakarJsonValueComparer<T>());
    }

    /// <summary> Stores every property of type <typeparamref name="T"/> as Jakar.Json text: <c>configurationBuilder.Properties&lt;Order&gt;().HaveJakarJsonConversion()</c>. </summary>
    public static PropertiesConfigurationBuilder<T> HaveJakarJsonConversion<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] T>( this PropertiesConfigurationBuilder<T> builder )
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.HaveConversion<JakarJsonValueConverter<T>, JakarJsonValueComparer<T>>();
    }
}



/// <summary> Jakar.Json text for <typeparamref name="T"/>. </summary>
internal static class JakarJsonText<T>
{
    private const int STACK_BYTES = 512;


    public static string Write( T? value )
    {
        using PooledBufferWriter buffer = new();
        JsonModels<T>.Write(value, buffer);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    public static T? Read( string text )
    {
        int     max    = Encoding.UTF8.GetMaxByteCount(text.Length);
        byte[]? rented = null;

        Span<byte> buffer = max <= STACK_BYTES
                                ? stackalloc byte[STACK_BYTES]
                                : rented = ArrayPool<byte>.Shared.Rent(max);

        try { return JsonModels<T>.Read(buffer[..Encoding.UTF8.GetBytes(text, buffer)]); }
        finally
        {
            if ( rented is not null ) { ArrayPool<byte>.Shared.Return(rented); }
        }
    }

    public static bool AreEqual( T? left, T? right )
    {
        if ( ReferenceEquals(left, right) ) { return true; }

        if ( left is null || right is null ) { return false; }

        using PooledBufferWriter a = new();
        using PooledBufferWriter b = new();
        JsonModels<T>.Write(left,  a);
        JsonModels<T>.Write(right, b);
        return a.WrittenSpan.SequenceEqual(b.WrittenSpan);
    }

    public static int Hash( T? value )
    {
        if ( value is null ) { return 0; }

        using PooledBufferWriter buffer = new();
        JsonModels<T>.Write(value, buffer);

        HashCode hash = new();
        hash.AddBytes(buffer.WrittenSpan);
        return hash.ToHashCode();
    }

    public static T? Copy( T? value )
    {
        if ( value is null ) { return default; }

        using PooledBufferWriter buffer = new();
        JsonModels<T>.Write(value, buffer);
        return JsonModels<T>.Read(buffer.WrittenSpan);
    }
}
