// Jakar.Json.Dapper
// 10/17/2026

using System.Data;
using System.Reflection;
using System.Text;
using Dapper;



namespace Jakar.Json.Dapper;


/// <summary> Writes a <typeparamref name="T"/> parameter as Jakar.Json text, and reads a JSON column (text or UTF-8 bytes) back into <typeparamref name="T"/>. </summary>
/// <exception cref="NotSupportedException"> Jakar.Json can't serialize <typeparamref name="T"/>. </exception>
public sealed class JakarJsonTypeHandler<T>() : SqlMapper.TypeHandler<T>
{
    private readonly JsonTypeCodec __codec = JsonModels<T>.RequiredCodec;


    public override void SetValue( IDbDataParameter parameter, T? value ) => JakarJsonTypeHandler.Set(parameter, __codec, value);

    public override T? Parse( object value ) => (T?)JakarJsonTypeHandler.Read(__codec, value);
}



/// <summary> <see cref="JakarJsonTypeHandler{T}"/> for a type known only at run time (what <see cref="JakarJsonDapper.AddTypeHandlers(Assembly)"/> registers). </summary>
public sealed class JakarJsonTypeHandler( Type type ) : SqlMapper.ITypeHandler
{
    private readonly JsonTypeCodec __codec = JsonModels.RequiredCodec(type);


    public void SetValue( IDbDataParameter parameter, object value ) => Set(parameter, __codec, value);

    public object? Parse( Type destinationType, object value ) => Read(__codec, value);


    internal static void Set( IDbDataParameter parameter, JsonTypeCodec codec, object? value )
    {
        parameter.DbType = DbType.String;

        if ( value is null or DBNull )
        {
            parameter.Value = DBNull.Value;
            return;
        }

        using PooledBufferWriter buffer = new();
        codec.Write(value, buffer);
        parameter.Value = Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    internal static object? Read( JsonTypeCodec codec, object value ) => value switch
                                                                        {
                                                                            null or DBNull => null,
                                                                            byte[] utf8    => codec.Read(utf8),
                                                                            string text    => codec.Read(Encoding.UTF8.GetBytes(text)),
                                                                            _              => codec.Read(Encoding.UTF8.GetBytes(value.ToString() ?? ""))
                                                                        };
}



public static class JakarJsonDapper
{
    /// <summary> Maps <typeparamref name="T"/> to a JSON column, process-wide (Dapper's type handlers are global). </summary>
    /// <exception cref="NotSupportedException"> Jakar.Json can't serialize <typeparamref name="T"/>. </exception>
    public static void AddTypeHandler<T>() => SqlMapper.AddTypeHandler(new JakarJsonTypeHandler<T>());

    /// <summary> Maps every <c>[JsonModel]</c> type declared in <paramref name="assembly"/> to a JSON column. Returns how many were added. </summary>
    [RequiresUnreferencedCode("Enumerates the assembly's types; use AddTypeHandler<T>() when trimming")]
    public static int AddTypeHandlers( Assembly assembly )
    {
        ArgumentNullException.ThrowIfNull(assembly);
        int added = 0;

        foreach ( Type type in assembly.GetTypes() )
        {
            if ( type.IsAbstract || type.IsGenericTypeDefinition || type.ContainsGenericParameters || !JsonModels.IsSupported(type) ) { continue; }

            SqlMapper.AddTypeHandler(type, new JakarJsonTypeHandler(type));
            added++;
        }

        return added;
    }
}
