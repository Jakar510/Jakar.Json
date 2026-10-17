// Jakar.Json integration tests (shared source, compiled into each test project)
// 10/17/2026

using System.Buffers;



namespace Jakar.Json.Tests.Shared;


/// <summary> Multi-segment <see cref="ReadOnlySequence{T}"/>s, as pipes and network buffers produce. </summary>
public static class Segments
{
    /// <summary> <paramref name="data"/> split into segments of <paramref name="size"/> bytes. </summary>
    public static ReadOnlySequence<byte> Split( byte[] data, int size )
    {
        if ( data.Length <= size ) { return new ReadOnlySequence<byte>(data); }

        Segment first = new(data.AsMemory(0, size), 0);
        Segment last  = first;

        for ( int offset = size; offset < data.Length; offset += size ) { last = last.Append(data.AsMemory(offset, Math.Min(size, data.Length - offset))); }

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }



    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment( ReadOnlyMemory<byte> memory, long runningIndex )
        {
            Memory       = memory;
            RunningIndex = runningIndex;
        }

        public Segment Append( ReadOnlyMemory<byte> memory )
        {
            Segment next = new(memory, RunningIndex + Memory.Length);
            Next = next;
            return next;
        }
    }
}
