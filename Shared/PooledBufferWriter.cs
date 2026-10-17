// Jakar.Json (shared source, compiled into each integration package)
// 10/17/2026

using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;



namespace Jakar.Json.Integration;


/// <summary>
///     An <see cref="IBufferWriter{T}"/> over <see cref="ArrayPool{T}.Shared"/>: the scratch buffer a payload is written into before it's copied out or handed on. Dispose it to return the
///     array. Unlike a thread-static buffer it's safe across <see langword="await"/>.
/// </summary>
internal sealed class PooledBufferWriter( int initialCapacity = 256 ) : IBufferWriter<byte>, IMemoryOwner<byte>
{
    private byte[] __buffer = ArrayPool<byte>.Shared.Rent(Math.Max(initialCapacity, 16));
    private int    __written;


    public ReadOnlySpan<byte>   WrittenSpan   => __buffer.AsSpan(0, __written);
    public ReadOnlyMemory<byte> WrittenMemory => __buffer.AsMemory(0, __written);
    public int                  WrittenCount  => __written;

    /// <summary> The written bytes, for a host that takes ownership of the buffer (and disposes it to return the array). </summary>
    Memory<byte> IMemoryOwner<byte>.Memory => __buffer.AsMemory(0, __written);


    public void Advance( int count )
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if ( count > __buffer.Length - __written ) { throw new InvalidOperationException("Advanced past the end of the buffer"); }

        __written += count;
    }

    public Memory<byte> GetMemory( int sizeHint = 0 )
    {
        Ensure(sizeHint);
        return __buffer.AsMemory(__written);
    }

    public Span<byte> GetSpan( int sizeHint = 0 )
    {
        Ensure(sizeHint);
        return __buffer.AsSpan(__written);
    }

    /// <summary> Appends everything left in <paramref name="stream"/>. Returns this buffer. </summary>
    public PooledBufferWriter ReadFrom( Stream stream )
    {
        int read;

        do
        {
            read = stream.Read(GetSpan(4096));
            Advance(read);
        }
        while ( read > 0 );

        return this;
    }

    /// <inheritdoc cref="ReadFrom(Stream)"/>
    public async ValueTask<PooledBufferWriter> ReadFromAsync( Stream stream, CancellationToken token = default )
    {
        int read;

        do
        {
            read = await stream.ReadAsync(GetMemory(4096), token).ConfigureAwait(false);
            Advance(read);
        }
        while ( read > 0 );

        return this;
    }

    public byte[] ToArray() => WrittenSpan.ToArray();

    public void Reset() => __written = 0;

    public void Dispose()
    {
        byte[] buffer = __buffer;
        __buffer  = [];
        __written = 0;
        if ( buffer.Length > 0 ) { ArrayPool<byte>.Shared.Return(buffer); }
    }


    private void Ensure( int sizeHint )
    {
        int needed = Math.Max(sizeHint, 1);
        if ( __buffer.Length - __written >= needed ) { return; }

        int    size   = Math.Max(__buffer.Length * 2, __written + needed);
        byte[] larger = ArrayPool<byte>.Shared.Rent(size);
        __buffer.AsSpan(0, __written).CopyTo(larger);
        if ( __buffer.Length > 0 ) { ArrayPool<byte>.Shared.Return(__buffer); }

        __buffer = larger;
    }
}
