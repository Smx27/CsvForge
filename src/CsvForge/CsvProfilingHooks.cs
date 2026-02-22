using System;
using System.Diagnostics;

namespace CsvForge;

internal static class CsvProfilingHooks
{
    internal static Action<SerializationProfile>? OnSerializationCompleted;

    internal static SerializationScope Start(int columnCount)
    {
        if (OnSerializationCompleted is null)
        {
            return default;
        }

        return new SerializationScope(GC.GetAllocatedBytesForCurrentThread(), Stopwatch.GetTimestamp(), columnCount);
    }

    internal readonly struct SerializationScope
    {
        private readonly long _allocatedBytes;
        private readonly long _startedAt;
        private readonly int _columnCount;

        public SerializationScope(long allocatedBytes, long startedAt, int columnCount)
        {
            _allocatedBytes = allocatedBytes;
            _startedAt = startedAt;
            _columnCount = columnCount;
        }

        public void Complete(int rowsWritten)
        {
            var callback = OnSerializationCompleted;
            if (callback is null)
            {
                return;
            }

            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - _allocatedBytes;
            var elapsed = Stopwatch.GetElapsedTime(_startedAt);
            callback(new SerializationProfile(rowsWritten, _columnCount, allocatedBytes, elapsed));
        }
    }
}

internal readonly record struct SerializationProfile(int RowsWritten, int ColumnCount, long AllocatedBytes, TimeSpan Elapsed);
