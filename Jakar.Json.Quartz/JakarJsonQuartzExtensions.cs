// Jakar.Json.Quartz
// 10/17/2026

using System.Text;
using Quartz;



namespace Jakar.Json.Quartz;


/// <summary>
///     Stores <c>[JsonModel]</c> values (and arrays, lists and collection interfaces of them) in Quartz job data as Jakar.Json text. Quartz' persistent stores only round-trip simple values
///     (strings, numbers, dates, ...), and expect anything structured to be stored as a string: these helpers do that, with any job store and serializer.
/// </summary>
public static class JakarJsonQuartzExtensions
{
    /// <summary> Stores <paramref name="value"/> under <paramref name="key"/> as Jakar.Json text. Returns <paramref name="map"/>. </summary>
    /// <exception cref="NotSupportedException"> Jakar.Json can't serialize <typeparamref name="T"/>. </exception>
    public static JobDataMap PutJakarJson<T>( this JobDataMap map, string key, T value )
    {
        ArgumentNullException.ThrowIfNull(map);

        using PooledBufferWriter buffer = new();
        JsonModels<T>.Write(value, buffer);
        map[key] = Encoding.UTF8.GetString(buffer.WrittenSpan);
        return map;
    }

    /// <summary> The <typeparamref name="T"/> stored under <paramref name="key"/> by <see cref="PutJakarJson{T}"/>; <see langword="default"/> when there's none. </summary>
    /// <exception cref="JsonReadException"> The stored text isn't valid JSON for <typeparamref name="T"/>. </exception>
    /// <exception cref="InvalidCastException"> The entry isn't text. </exception>
    public static T? GetJakarJson<T>( this JobDataMap map, string key )
    {
        ArgumentNullException.ThrowIfNull(map);
        if ( !map.TryGetValue(key, out object? value) || value is null ) { return default; }

        return value is string text
                   ? JsonModels<T>.Read(Encoding.UTF8.GetBytes(text))
                   : throw new InvalidCastException($"Job data '{key}' is a {value.GetType()}, not Jakar.Json text");
    }

    /// <summary> Adds <paramref name="value"/> to the job's data as Jakar.Json text. Returns <paramref name="builder"/>. </summary>
    public static JobBuilder<TJob> UsingJakarJson<TJob, T>( this JobBuilder<TJob> builder, string key, T value )
        where TJob : IJob
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UsingJobData(new JobDataMap().PutJakarJson(key, value));
    }
}
