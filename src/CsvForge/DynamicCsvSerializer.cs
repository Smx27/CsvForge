using System;
using System.Buffers;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvForge.Metadata;

namespace CsvForge;

internal static class DynamicCsvSerializer
{
    internal interface IReplayableAsyncEnumerable<out T>
    {
        IAsyncEnumerable<T> Replay();
    }

    public static bool CanHandle<T>()
    {
        var type = typeof(T);
        return type == typeof(object)
            || type == typeof(ExpandoObject)
            || typeof(IDictionary<string, object?>).IsAssignableFrom(type);
    }

    public static int Write<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options)
    {
        using var context = new CsvSerializationContext(options);
        return options.HeterogeneousHeaderBehavior == CsvHeterogeneousHeaderBehavior.FirstShapeLock
            ? WriteFirstShapeLock(data, writer, options, context)
            : WriteUnion(data, writer, options, context);
    }

    public static async Task<int> WriteAsync<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
    {
        using var context = new CsvSerializationContext(options);
        return options.HeterogeneousHeaderBehavior == CsvHeterogeneousHeaderBehavior.FirstShapeLock
            ? await WriteFirstShapeLockAsync(data, writer, options, context, cancellationToken).ConfigureAwait(false)
            : await WriteUnionAsync(data, writer, options, context, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<int> WriteAsync<T>(IAsyncEnumerable<T> data, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
    {
        using var context = new CsvSerializationContext(options);
        if (options.HeterogeneousHeaderBehavior == CsvHeterogeneousHeaderBehavior.FirstShapeLock)
        {
            return await WriteFirstShapeLockAsync(data, writer, options, context, cancellationToken).ConfigureAwait(false);
        }

        if (data is IReplayableAsyncEnumerable<T> replayable)
        {
            return await WriteUnionReplayableAsync(replayable, writer, options, context, cancellationToken).ConfigureAwait(false);
        }

        if (options.UnionAsyncBehavior == CsvUnionAsyncBehavior.FirstShapeLock)
        {
            return await WriteFirstShapeLockAsync(data, writer, options, context, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("Union header discovery for IAsyncEnumerable dynamic rows requires a replayable source. Set CsvOptions.UnionAsyncBehavior to FirstShapeLock to stream non-replayable sources.");
    }

    private static int WriteUnion<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options, CsvSerializationContext context)
    {
        var headers = new List<string>();
        var headerSet = new HashSet<string>(StringComparer.Ordinal);
        var rowsWritten = 0;

        foreach (var item in data)
        {
            rowsWritten++;
            CollectHeaders(DynamicRow.Create(item), headers, headerSet, allowAdd: true);
        }

        if (options.IncludeHeader)
        {
            WriteHeader(writer, headers, context, options.NewLine);
        }

        foreach (var item in data)
        {
            WriteRow(writer, DynamicRow.Create(item), headers, context, options.NewLine);
        }

        return rowsWritten;
    }

    private static async Task<int> WriteUnionAsync<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options, CsvSerializationContext context, CancellationToken cancellationToken)
    {
        var headers = new List<string>();
        var headerSet = new HashSet<string>(StringComparer.Ordinal);
        var rowsWritten = 0;

        foreach (var item in data)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowsWritten++;
            CollectHeaders(DynamicRow.Create(item), headers, headerSet, allowAdd: true);
        }

        if (options.IncludeHeader)
        {
            await WriteHeaderAsync(writer, headers, context, options.NewLine, cancellationToken).ConfigureAwait(false);
        }

        foreach (var item in data)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteRowAsync(writer, DynamicRow.Create(item), headers, context, options.NewLine, cancellationToken).ConfigureAwait(false);
        }

        return rowsWritten;
    }

    private static async Task<int> WriteUnionReplayableAsync<T>(IReplayableAsyncEnumerable<T> replayable, TextWriter writer, CsvOptions options, CsvSerializationContext context, CancellationToken cancellationToken)
    {
        var headers = new List<string>();
        var headerSet = new HashSet<string>(StringComparer.Ordinal);
        var rowsWritten = 0;

        await foreach (var item in replayable.Replay().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            rowsWritten++;
            CollectHeaders(DynamicRow.Create(item), headers, headerSet, allowAdd: true);
        }

        if (options.IncludeHeader)
        {
            await WriteHeaderAsync(writer, headers, context, options.NewLine, cancellationToken).ConfigureAwait(false);
        }

        await foreach (var item in replayable.Replay().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await WriteRowAsync(writer, DynamicRow.Create(item), headers, context, options.NewLine, cancellationToken).ConfigureAwait(false);
        }

        return rowsWritten;
    }

    private static int WriteFirstShapeLock<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options, CsvSerializationContext context)
    {
        using var enumerator = data.GetEnumerator();
        var headers = new List<string>();
        var headerSet = new HashSet<string>(StringComparer.Ordinal);
        var rowsWritten = 0;
        var pendingEmptyRows = 0;
        var headerWritten = false;

        while (enumerator.MoveNext())
        {
            rowsWritten++;
            var row = DynamicRow.Create(enumerator.Current);

            if (headerSet.Count == 0 && !TryLockHeaders(row, headers, headerSet))
            {
                pendingEmptyRows++;
                continue;
            }

            if (!headerWritten)
            {
                if (options.IncludeHeader)
                {
                    WriteHeader(writer, headers, context, options.NewLine);
                }

                WriteEmptyRows(writer, headers.Count, pendingEmptyRows, context, options.NewLine);
                headerWritten = true;
                pendingEmptyRows = 0;
            }

            WriteRow(writer, row, headers, context, options.NewLine);
        }

        if (!headerWritten)
        {
            if (options.IncludeHeader)
            {
                WriteHeader(writer, headers, context, options.NewLine);
            }

            WriteEmptyRows(writer, headers.Count, pendingEmptyRows, context, options.NewLine);
        }

        return rowsWritten;
    }

    private static async Task<int> WriteFirstShapeLockAsync<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options, CsvSerializationContext context, CancellationToken cancellationToken)
    {
        using var enumerator = data.GetEnumerator();
        var headers = new List<string>();
        var headerSet = new HashSet<string>(StringComparer.Ordinal);
        var rowsWritten = 0;
        var pendingEmptyRows = 0;
        var headerWritten = false;

        while (enumerator.MoveNext())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rowsWritten++;
            var row = DynamicRow.Create(enumerator.Current);

            if (headerSet.Count == 0 && !TryLockHeaders(row, headers, headerSet))
            {
                pendingEmptyRows++;
                continue;
            }

            if (!headerWritten)
            {
                if (options.IncludeHeader)
                {
                    await WriteHeaderAsync(writer, headers, context, options.NewLine, cancellationToken).ConfigureAwait(false);
                }

                await WriteEmptyRowsAsync(writer, headers.Count, pendingEmptyRows, context, options.NewLine, cancellationToken).ConfigureAwait(false);
                headerWritten = true;
                pendingEmptyRows = 0;
            }

            await WriteRowAsync(writer, row, headers, context, options.NewLine, cancellationToken).ConfigureAwait(false);
        }

        if (!headerWritten)
        {
            if (options.IncludeHeader)
            {
                await WriteHeaderAsync(writer, headers, context, options.NewLine, cancellationToken).ConfigureAwait(false);
            }

            await WriteEmptyRowsAsync(writer, headers.Count, pendingEmptyRows, context, options.NewLine, cancellationToken).ConfigureAwait(false);
        }

        return rowsWritten;
    }

    private static async Task<int> WriteFirstShapeLockAsync<T>(IAsyncEnumerable<T> data, TextWriter writer, CsvOptions options, CsvSerializationContext context, CancellationToken cancellationToken)
    {
        var headers = new List<string>();
        var headerSet = new HashSet<string>(StringComparer.Ordinal);
        var rowsWritten = 0;
        var pendingEmptyRows = 0;
        var headerWritten = false;

        await foreach (var item in data.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            rowsWritten++;
            var row = DynamicRow.Create(item);

            if (headerSet.Count == 0 && !TryLockHeaders(row, headers, headerSet))
            {
                pendingEmptyRows++;
                continue;
            }

            if (!headerWritten)
            {
                if (options.IncludeHeader)
                {
                    await WriteHeaderAsync(writer, headers, context, options.NewLine, cancellationToken).ConfigureAwait(false);
                }

                await WriteEmptyRowsAsync(writer, headers.Count, pendingEmptyRows, context, options.NewLine, cancellationToken).ConfigureAwait(false);
                headerWritten = true;
                pendingEmptyRows = 0;
            }

            await WriteRowAsync(writer, row, headers, context, options.NewLine, cancellationToken).ConfigureAwait(false);
        }

        if (!headerWritten)
        {
            if (options.IncludeHeader)
            {
                await WriteHeaderAsync(writer, headers, context, options.NewLine, cancellationToken).ConfigureAwait(false);
            }

            await WriteEmptyRowsAsync(writer, headers.Count, pendingEmptyRows, context, options.NewLine, cancellationToken).ConfigureAwait(false);
        }

        return rowsWritten;
    }

    private static bool TryLockHeaders(DynamicRow row, List<string> headers, HashSet<string> headerSet)
    {
        var before = headers.Count;
        CollectHeaders(row, headers, headerSet, allowAdd: true);
        return headers.Count > before;
    }

    private static void CollectHeaders(DynamicRow row, List<string> headers, HashSet<string> headerSet, bool allowAdd)
    {
        if (!allowAdd)
        {
            return;
        }

        foreach (var key in row.GetKeys())
        {
            if (headerSet.Add(key))
            {
                headers.Add(key);
            }
        }
    }

    private static void WriteHeader(TextWriter writer, List<string> headers, CsvSerializationContext context, string newLine)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (i > 0)
            {
                writer.Write(context.Options.Delimiter);
            }

            CsvValueFormatter.WriteField(writer, headers[i], context);
        }

        writer.Write(newLine);
    }

    private static async Task WriteHeaderAsync(TextWriter writer, List<string> headers, CsvSerializationContext context, string newLine, CancellationToken cancellationToken)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (i > 0)
            {
                await writer.WriteAsync(context.Options.Delimiter).ConfigureAwait(false);
            }

            await CsvValueFormatter.WriteFieldAsync(writer, headers[i], context, cancellationToken).ConfigureAwait(false);
        }

        await writer.WriteAsync(newLine.AsMemory()).ConfigureAwait(false);
    }

    private static void WriteRow(TextWriter writer, DynamicRow row, List<string> headers, CsvSerializationContext context, string newLine)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (i > 0)
            {
                writer.Write(context.Options.Delimiter);
            }

            CsvValueFormatter.WriteField(writer, row.GetValue(headers[i]), context);
        }

        writer.Write(newLine);
    }

    private static async Task WriteRowAsync(TextWriter writer, DynamicRow row, List<string> headers, CsvSerializationContext context, string newLine, CancellationToken cancellationToken)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (i > 0)
            {
                await writer.WriteAsync(context.Options.Delimiter).ConfigureAwait(false);
            }

            await CsvValueFormatter.WriteFieldAsync(writer, row.GetValue(headers[i]), context, cancellationToken).ConfigureAwait(false);
        }

        await writer.WriteAsync(newLine.AsMemory()).ConfigureAwait(false);
    }

    private static void WriteEmptyRows(TextWriter writer, int headerCount, int rowCount, CsvSerializationContext context, string newLine)
    {
        if (rowCount <= 0)
        {
            return;
        }

        var rented = ArrayPool<string>.Shared.Rent(Math.Max(headerCount, 1));
        try
        {
            Array.Fill(rented, string.Empty, 0, headerCount);
            for (var i = 0; i < rowCount; i++)
            {
                WriteRow(writer, new DynamicRow(rented, headerCount), headerCount, context, newLine);
            }
        }
        finally
        {
            ArrayPool<string>.Shared.Return(rented, clearArray: true);
        }
    }

    private static async Task WriteEmptyRowsAsync(TextWriter writer, int headerCount, int rowCount, CsvSerializationContext context, string newLine, CancellationToken cancellationToken)
    {
        if (rowCount <= 0)
        {
            return;
        }

        var rented = ArrayPool<string>.Shared.Rent(Math.Max(headerCount, 1));
        try
        {
            Array.Fill(rented, string.Empty, 0, headerCount);
            for (var i = 0; i < rowCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await WriteRowAsync(writer, new DynamicRow(rented, headerCount), headerCount, context, newLine, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            ArrayPool<string>.Shared.Return(rented, clearArray: true);
        }
    }

    private static void WriteRow(TextWriter writer, DynamicRow row, int headerCount, CsvSerializationContext context, string newLine)
    {
        for (var i = 0; i < headerCount; i++)
        {
            if (i > 0)
            {
                writer.Write(context.Options.Delimiter);
            }

            CsvValueFormatter.WriteField(writer, row.GetValue(i), context);
        }

        writer.Write(newLine);
    }

    private static async Task WriteRowAsync(TextWriter writer, DynamicRow row, int headerCount, CsvSerializationContext context, string newLine, CancellationToken cancellationToken)
    {
        for (var i = 0; i < headerCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (i > 0)
            {
                await writer.WriteAsync(context.Options.Delimiter).ConfigureAwait(false);
            }

            await CsvValueFormatter.WriteFieldAsync(writer, row.GetValue(i), context, cancellationToken).ConfigureAwait(false);
        }

        await writer.WriteAsync(newLine.AsMemory()).ConfigureAwait(false);
    }

    private sealed class DynamicRow
    {
        private readonly IDictionary<string, object?>? _dictionary;
        private readonly RuntimeTypeMetadata? _runtimeMetadata;
        private readonly object? _instance;
        private readonly string[]? _arrayValues;
        private readonly int _arrayCount;

        public DynamicRow(string[] arrayValues, int arrayCount)
        {
            _arrayValues = arrayValues;
            _arrayCount = arrayCount;
        }

        private DynamicRow(IDictionary<string, object?> dictionary)
        {
            _dictionary = dictionary;
        }

        private DynamicRow(object instance, RuntimeTypeMetadata runtimeMetadata)
        {
            _instance = instance;
            _runtimeMetadata = runtimeMetadata;
        }

        public static DynamicRow Create<T>(T item)
        {
            if (item is IDictionary<string, object?> dictionary)
            {
                return new DynamicRow(dictionary);
            }

            if (item is null)
            {
                return new DynamicRow(new Dictionary<string, object?>(StringComparer.Ordinal));
            }

            var runtimeType = item.GetType();
            var metadata = TypeMetadataCache.GetOrAddRuntime(runtimeType);
            return new DynamicRow(item, metadata);
        }

        public IEnumerable<string> GetKeys()
        {
            if (_dictionary is not null)
            {
                foreach (var key in _dictionary.Keys)
                {
                    yield return key;
                }

                yield break;
            }

            if (_arrayValues is not null)
            {
                yield break;
            }

            foreach (var column in _runtimeMetadata!.Columns)
            {
                yield return column.ColumnName;
            }
        }

        public object? GetValue(string key)
        {
            if (_dictionary is not null)
            {
                return _dictionary.TryGetValue(key, out var value) ? value : null;
            }

            if (_arrayValues is not null)
            {
                return null;
            }

            return _runtimeMetadata!.ColumnLookup.TryGetValue(key, out var getter)
                ? getter(_instance!)
                : null;
        }

        public object? GetValue(int index)
        {
            if (_arrayValues is null || index >= _arrayCount)
            {
                return null;
            }

            return _arrayValues[index];
        }
    }
}
