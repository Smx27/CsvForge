using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsvForge.Metadata;

namespace CsvForge;

internal static class CsvSerializer
{
    public static void Write<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        var metadata = TypeMetadataCache.GetOrAdd(typeof(T));

        if (options.IncludeHeader)
        {
            WriteHeader(writer, metadata, options);
        }

        foreach (var item in data)
        {
            WriteRecord(writer, item, metadata, options);
        }
    }

    public static async Task WriteAsync<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        var metadata = TypeMetadataCache.GetOrAdd(typeof(T));

        if (options.IncludeHeader)
        {
            await WriteHeaderAsync(writer, metadata, options, cancellationToken).ConfigureAwait(false);
        }

        foreach (var item in data)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteRecordAsync(writer, item, metadata, options, cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task WriteAsync<T>(IAsyncEnumerable<T> data, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        var metadata = TypeMetadataCache.GetOrAdd(typeof(T));

        if (options.IncludeHeader)
        {
            await WriteHeaderAsync(writer, metadata, options, cancellationToken).ConfigureAwait(false);
        }

        await foreach (var item in data.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await WriteRecordAsync(writer, item, metadata, options, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void WriteHeader(TextWriter writer, TypeMetadata metadata, CsvOptions options)
    {
        for (var i = 0; i < metadata.Columns.Length; i++)
        {
            if (i > 0)
            {
                writer.Write(options.Delimiter);
            }

            WriteField(writer, metadata.Columns[i].ColumnName, options);
        }

        writer.Write(options.NewLine);
    }

    private static async Task WriteHeaderAsync(TextWriter writer, TypeMetadata metadata, CsvOptions options, CancellationToken cancellationToken)
    {
        for (var i = 0; i < metadata.Columns.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (i > 0)
            {
                await writer.WriteAsync(options.Delimiter).ConfigureAwait(false);
            }

            await WriteFieldAsync(writer, metadata.Columns[i].ColumnName, options, cancellationToken).ConfigureAwait(false);
        }

        await writer.WriteAsync(options.NewLine).ConfigureAwait(false);
    }

    private static void WriteRecord<T>(TextWriter writer, T item, TypeMetadata metadata, CsvOptions options)
    {
        for (var i = 0; i < metadata.Columns.Length; i++)
        {
            if (i > 0)
            {
                writer.Write(options.Delimiter);
            }

            var column = metadata.Columns[i];
            var value = column.Getter(item!);
            WriteField(writer, column.Formatter(value, options.FormatProvider), options);
        }

        writer.Write(options.NewLine);
    }

    private static async Task WriteRecordAsync<T>(TextWriter writer, T item, TypeMetadata metadata, CsvOptions options, CancellationToken cancellationToken)
    {
        for (var i = 0; i < metadata.Columns.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (i > 0)
            {
                await writer.WriteAsync(options.Delimiter).ConfigureAwait(false);
            }

            var column = metadata.Columns[i];
            var value = column.Getter(item!);
            await WriteFieldAsync(writer, column.Formatter(value, options.FormatProvider), options, cancellationToken).ConfigureAwait(false);
        }

        await writer.WriteAsync(options.NewLine).ConfigureAwait(false);
    }

    private static void WriteField(TextWriter writer, string? value, CsvOptions options)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (!NeedsEscaping(value, options.Delimiter, options.NewLine))
        {
            writer.Write(value);
            return;
        }

        writer.Write('"');
        var start = 0;
        while (true)
        {
            var quoteIndex = value.IndexOf('"', start);
            if (quoteIndex < 0)
            {
                writer.Write(value.AsSpan(start));
                break;
            }

            writer.Write(value.AsSpan(start, quoteIndex - start));
            writer.Write("\"\"");
            start = quoteIndex + 1;
        }

        writer.Write('"');
    }

    private static async Task WriteFieldAsync(TextWriter writer, string? value, CsvOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (!NeedsEscaping(value, options.Delimiter, options.NewLine))
        {
            await writer.WriteAsync(value).ConfigureAwait(false);
            return;
        }

        await writer.WriteAsync("\"").ConfigureAwait(false);
        var start = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var quoteIndex = value.IndexOf('"', start);
            if (quoteIndex < 0)
            {
                await writer.WriteAsync(value.AsMemory(start)).ConfigureAwait(false);
                break;
            }

            await writer.WriteAsync(value.AsMemory(start, quoteIndex - start)).ConfigureAwait(false);
            await writer.WriteAsync("\"\"").ConfigureAwait(false);
            start = quoteIndex + 1;
        }

        await writer.WriteAsync("\"").ConfigureAwait(false);
    }

    private static bool NeedsEscaping(string value, char delimiter, string newLine)
    {
        if (value.IndexOfAny(SearchValues.Create([delimiter, '"', '\r', '\n'])) >= 0)
        {
            return true;
        }

        return !string.IsNullOrEmpty(newLine) && value.Contains(newLine, StringComparison.Ordinal);
    }
}
