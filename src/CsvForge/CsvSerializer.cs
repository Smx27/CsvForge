using System;
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

        if (DynamicCsvSerializer.CanHandle<T>())
        {
            var rowsWritten = DynamicCsvSerializer.Write(data, writer, options);
            var profileScope = CsvProfilingHooks.Start(0);
            profileScope.Complete(rowsWritten);
            return;
        }

        var metadata = TypeMetadataCache.GetOrAdd<T>();
        var newLine = options.NewLine;
        using var context = new CsvSerializationContext(options);

        if (options.IncludeHeader)
        {
            WriteHeader(writer, metadata, context, newLine);
        }

        var profileScope = CsvProfilingHooks.Start(metadata.Columns.Length);
        var rowsWritten = WriteRows(data, writer, metadata, context, newLine);
        profileScope.Complete(rowsWritten);
    }

    public static async Task WriteAsync<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        if (DynamicCsvSerializer.CanHandle<T>())
        {
            var rowsWritten = await DynamicCsvSerializer.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
            var profileScope = CsvProfilingHooks.Start(0);
            profileScope.Complete(rowsWritten);
            return;
        }

        var metadata = TypeMetadataCache.GetOrAdd<T>();
        var newLine = options.NewLine;
        using var context = new CsvSerializationContext(options);

        if (options.IncludeHeader)
        {
            await WriteHeaderAsync(writer, metadata, context, newLine, cancellationToken).ConfigureAwait(false);
        }

        var profileScope = CsvProfilingHooks.Start(metadata.Columns.Length);
        var rowsWritten = await WriteRowsAsync(data, writer, metadata, context, newLine, cancellationToken).ConfigureAwait(false);
        profileScope.Complete(rowsWritten);
    }

    public static async Task WriteAsync<T>(IAsyncEnumerable<T> data, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        if (DynamicCsvSerializer.CanHandle<T>())
        {
            var rowsWritten = await DynamicCsvSerializer.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
            var profileScope = CsvProfilingHooks.Start(0);
            profileScope.Complete(rowsWritten);
            return;
        }

        var metadata = TypeMetadataCache.GetOrAdd<T>();
        var newLine = options.NewLine;
        using var context = new CsvSerializationContext(options);

        if (options.IncludeHeader)
        {
            await WriteHeaderAsync(writer, metadata, context, newLine, cancellationToken).ConfigureAwait(false);
        }

        var profileScope = CsvProfilingHooks.Start(metadata.Columns.Length);
        var rowsWritten = 0;
        await foreach (var item in data.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await WriteRecordAsync(writer, item, metadata, context, newLine, cancellationToken).ConfigureAwait(false);
            rowsWritten++;
        }

        profileScope.Complete(rowsWritten);
    }

    private static int WriteRows<T>(IEnumerable<T> data, TextWriter writer, TypeMetadata<T> metadata, CsvSerializationContext context, string newLine)
    {
        if (data is T[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                WriteRecord(writer, array[i], metadata, context, newLine);
            }

            return array.Length;
        }

        if (data is List<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                WriteRecord(writer, list[i], metadata, context, newLine);
            }

            return list.Count;
        }

        var rowsWritten = 0;
        foreach (var item in data)
        {
            WriteRecord(writer, item, metadata, context, newLine);
            rowsWritten++;
        }

        return rowsWritten;
    }

    private static async Task<int> WriteRowsAsync<T>(IEnumerable<T> data, TextWriter writer, TypeMetadata<T> metadata, CsvSerializationContext context, string newLine, CancellationToken cancellationToken)
    {
        if (data is T[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await WriteRecordAsync(writer, array[i], metadata, context, newLine, cancellationToken).ConfigureAwait(false);
            }

            return array.Length;
        }

        if (data is List<T> list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await WriteRecordAsync(writer, list[i], metadata, context, newLine, cancellationToken).ConfigureAwait(false);
            }

            return list.Count;
        }

        var rowsWritten = 0;
        foreach (var item in data)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteRecordAsync(writer, item, metadata, context, newLine, cancellationToken).ConfigureAwait(false);
            rowsWritten++;
        }

        return rowsWritten;
    }

    private static void WriteHeader<T>(TextWriter writer, TypeMetadata<T> metadata, CsvSerializationContext context, string newLine)
    {
        for (var i = 0; i < metadata.Columns.Length; i++)
        {
            if (i > 0)
            {
                writer.Write(context.Options.Delimiter);
            }

            CsvValueFormatter.WriteField(writer, metadata.Columns[i].ColumnName, context);
        }

        writer.Write(newLine);
    }

    private static async Task WriteHeaderAsync<T>(TextWriter writer, TypeMetadata<T> metadata, CsvSerializationContext context, string newLine, CancellationToken cancellationToken)
    {
        for (var i = 0; i < metadata.Columns.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (i > 0)
            {
                await writer.WriteAsync(context.Options.Delimiter).ConfigureAwait(false);
            }

            await CsvValueFormatter.WriteFieldAsync(writer, metadata.Columns[i].ColumnName, context, cancellationToken).ConfigureAwait(false);
        }

        await writer.WriteAsync(newLine.AsMemory()).ConfigureAwait(false);
    }

    private static void WriteRecord<T>(TextWriter writer, T item, TypeMetadata<T> metadata, CsvSerializationContext context, string newLine)
    {
        for (var i = 0; i < metadata.Columns.Length; i++)
        {
            if (i > 0)
            {
                writer.Write(context.Options.Delimiter);
            }

            metadata.Columns[i].Write(writer, item, context);
        }

        writer.Write(newLine);
    }

    private static async Task WriteRecordAsync<T>(TextWriter writer, T item, TypeMetadata<T> metadata, CsvSerializationContext context, string newLine, CancellationToken cancellationToken)
    {
        for (var i = 0; i < metadata.Columns.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (i > 0)
            {
                await writer.WriteAsync(context.Options.Delimiter).ConfigureAwait(false);
            }

            await metadata.Columns[i].WriteAsync(writer, item, context, cancellationToken).ConfigureAwait(false);
        }

        await writer.WriteAsync(newLine.AsMemory()).ConfigureAwait(false);
    }
}
