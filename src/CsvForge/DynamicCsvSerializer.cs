using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvForge.Metadata;

namespace CsvForge;

internal static class DynamicCsvSerializer
{
    public static bool CanHandle<T>()
    {
        var type = typeof(T);
        return type == typeof(object)
            || type == typeof(ExpandoObject)
            || typeof(IDictionary<string, object?>).IsAssignableFrom(type);
    }

    public static int Write<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options)
    {
        var rows = new List<DynamicRow>();
        var headers = new List<string>();
        var headerSet = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in data)
        {
            var row = DynamicRow.Create(item);
            rows.Add(row);
            CollectHeaders(row, headers, headerSet, options.HeterogeneousHeaderBehavior == CsvHeterogeneousHeaderBehavior.Union || headers.Count == 0);
        }

        using var context = new CsvSerializationContext(options);
        if (options.IncludeHeader)
        {
            WriteHeader(writer, headers, context, options.NewLine);
        }

        foreach (var row in rows)
        {
            WriteRow(writer, row, headers, context, options.NewLine);
        }

        return rows.Count;
    }

    public static async Task<int> WriteAsync<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
    {
        var rows = new List<DynamicRow>();
        var headers = new List<string>();
        var headerSet = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in data)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var row = DynamicRow.Create(item);
            rows.Add(row);
            CollectHeaders(row, headers, headerSet, options.HeterogeneousHeaderBehavior == CsvHeterogeneousHeaderBehavior.Union || headers.Count == 0);
        }

        using var context = new CsvSerializationContext(options);
        if (options.IncludeHeader)
        {
            await WriteHeaderAsync(writer, headers, context, options.NewLine, cancellationToken).ConfigureAwait(false);
        }

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteRowAsync(writer, row, headers, context, options.NewLine, cancellationToken).ConfigureAwait(false);
        }

        return rows.Count;
    }

    public static async Task<int> WriteAsync<T>(IAsyncEnumerable<T> data, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
    {
        var rows = new List<DynamicRow>();
        var headers = new List<string>();
        var headerSet = new HashSet<string>(StringComparer.Ordinal);

        await foreach (var item in data.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var row = DynamicRow.Create(item);
            rows.Add(row);
            CollectHeaders(row, headers, headerSet, options.HeterogeneousHeaderBehavior == CsvHeterogeneousHeaderBehavior.Union || headers.Count == 0);
        }

        using var context = new CsvSerializationContext(options);
        if (options.IncludeHeader)
        {
            await WriteHeaderAsync(writer, headers, context, options.NewLine, cancellationToken).ConfigureAwait(false);
        }

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteRowAsync(writer, row, headers, context, options.NewLine, cancellationToken).ConfigureAwait(false);
        }

        return rows.Count;
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

    private sealed class DynamicRow
    {
        private readonly IDictionary<string, object?>? _dictionary;
        private readonly RuntimeTypeMetadata? _runtimeMetadata;
        private readonly object? _instance;

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

            return _runtimeMetadata!.ColumnLookup.TryGetValue(key, out var getter)
                ? getter(_instance!)
                : null;
        }
    }
}
