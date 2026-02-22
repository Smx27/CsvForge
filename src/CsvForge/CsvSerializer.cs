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
    private const int InitialFormatBufferSize = 128;

    public static void Write<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        var metadata = TypeMetadataCache.GetOrAdd(typeof(T));
        var newLine = options.NewLine;

        if (options.IncludeHeader)
        {
            WriteHeader(writer, metadata, options, newLine);
        }

        foreach (var item in data)
        {
            WriteRecord(writer, item, metadata, options, newLine);
        }
    }

    public static async Task WriteAsync<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        var metadata = TypeMetadataCache.GetOrAdd(typeof(T));
        var newLine = options.NewLine;

        if (options.IncludeHeader)
        {
            await WriteHeaderAsync(writer, metadata, options, newLine, cancellationToken).ConfigureAwait(false);
        }

        foreach (var item in data)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteRecordAsync(writer, item, metadata, options, newLine, cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task WriteAsync<T>(IAsyncEnumerable<T> data, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        var metadata = TypeMetadataCache.GetOrAdd(typeof(T));
        var newLine = options.NewLine;

        if (options.IncludeHeader)
        {
            await WriteHeaderAsync(writer, metadata, options, newLine, cancellationToken).ConfigureAwait(false);
        }

        await foreach (var item in data.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await WriteRecordAsync(writer, item, metadata, options, newLine, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void WriteHeader(TextWriter writer, TypeMetadata metadata, CsvOptions options, string newLine)
    {
        for (var i = 0; i < metadata.Columns.Length; i++)
        {
            if (i > 0)
            {
                writer.Write(options.Delimiter);
            }

            WriteField(writer, metadata.Columns[i].ColumnName, options);
        }

        WriteNewLine(writer, newLine);
    }

    private static async Task WriteHeaderAsync(TextWriter writer, TypeMetadata metadata, CsvOptions options, string newLine, CancellationToken cancellationToken)
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

        await WriteNewLineAsync(writer, newLine).ConfigureAwait(false);
    }

    private static void WriteRecord<T>(TextWriter writer, T item, TypeMetadata metadata, CsvOptions options, string newLine)
    {
        for (var i = 0; i < metadata.Columns.Length; i++)
        {
            if (i > 0)
            {
                writer.Write(options.Delimiter);
            }

            var column = metadata.Columns[i];
            var value = column.Getter(item!);
            WriteField(writer, value, options);
        }

        WriteNewLine(writer, newLine);
    }

    private static async Task WriteRecordAsync<T>(TextWriter writer, T item, TypeMetadata metadata, CsvOptions options, string newLine, CancellationToken cancellationToken)
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
            await WriteFieldAsync(writer, value, options, cancellationToken).ConfigureAwait(false);
        }

        await WriteNewLineAsync(writer, newLine).ConfigureAwait(false);
    }

    private static void WriteField(TextWriter writer, object? value, CsvOptions options)
    {
        if (value is null)
        {
            return;
        }

        if (value is string text)
        {
            WriteEscapedString(writer, text, options.Delimiter);
            return;
        }

        var pooledBuffer = ArrayPool<char>.Shared.Rent(InitialFormatBufferSize);
        try
        {
            var charsWritten = TryFormatValueToBuffer(value, options.FormatProvider, ref pooledBuffer);
            WriteEscapedSpan(writer, pooledBuffer.AsSpan(0, charsWritten), options.Delimiter);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(pooledBuffer);
        }
    }

    private static async Task WriteFieldAsync(TextWriter writer, object? value, CsvOptions options, CancellationToken cancellationToken)
    {
        if (value is null)
        {
            return;
        }

        if (value is string text)
        {
            await WriteEscapedStringAsync(writer, text, options.Delimiter, cancellationToken).ConfigureAwait(false);
            return;
        }

        var pooledBuffer = ArrayPool<char>.Shared.Rent(InitialFormatBufferSize);
        try
        {
            var charsWritten = TryFormatValueToBuffer(value, options.FormatProvider, ref pooledBuffer);
            await WriteEscapedMemoryAsync(writer, pooledBuffer.AsMemory(0, charsWritten), options.Delimiter, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(pooledBuffer);
        }
    }

    private static int TryFormatValueToBuffer(object value, IFormatProvider formatProvider, ref char[] buffer)
    {
        if (value is ISpanFormattable spanFormattable)
        {
            while (true)
            {
                if (spanFormattable.TryFormat(buffer.AsSpan(), out var charsWritten, default, formatProvider))
                {
                    return charsWritten;
                }

                if (buffer.Length >= 16 * 1024)
                {
                    break;
                }

                GrowBuffer(ref buffer, buffer.Length * 2);
            }
        }

        if (value is IFormattable formattable)
        {
            var formatted = formattable.ToString(null, formatProvider) ?? string.Empty;
            EnsureCapacity(formatted.Length, ref buffer);
            formatted.AsSpan().CopyTo(buffer);
            return formatted.Length;
        }

        var converted = Convert.ToString(value, formatProvider) ?? string.Empty;
        EnsureCapacity(converted.Length, ref buffer);
        converted.AsSpan().CopyTo(buffer);
        return converted.Length;
    }

    private static void EnsureCapacity(int requiredLength, ref char[] buffer)
    {
        if (requiredLength <= buffer.Length)
        {
            return;
        }

        ArrayPool<char>.Shared.Return(buffer);
        buffer = ArrayPool<char>.Shared.Rent(requiredLength);
    }

    private static void GrowBuffer(ref char[] buffer, int targetSize)
    {
        var newBuffer = ArrayPool<char>.Shared.Rent(targetSize);
        ArrayPool<char>.Shared.Return(buffer);
        buffer = newBuffer;
    }

    private static void WriteEscapedString(TextWriter writer, string value, char delimiter)
    {
        if (value.Length == 0)
        {
            return;
        }

        WriteEscapedSpan(writer, value.AsSpan(), delimiter);
    }

    private static void WriteEscapedSpan(TextWriter writer, ReadOnlySpan<char> value, char delimiter)
    {
        if (!NeedsEscaping(value, delimiter))
        {
            writer.Write(value);
            return;
        }

        writer.Write('"');
        var start = 0;
        while (true)
        {
            var quoteIndex = value[start..].IndexOf('"');
            if (quoteIndex < 0)
            {
                writer.Write(value[start..]);
                break;
            }

            var absoluteQuoteIndex = start + quoteIndex;
            writer.Write(value[start..absoluteQuoteIndex]);
            writer.Write("\"\"");
            start = absoluteQuoteIndex + 1;
        }

        writer.Write('"');
    }

    private static async Task WriteEscapedStringAsync(TextWriter writer, string value, char delimiter, CancellationToken cancellationToken)
    {
        if (value.Length == 0)
        {
            return;
        }

        await WriteEscapedMemoryAsync(writer, value.AsMemory(), delimiter, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteEscapedMemoryAsync(TextWriter writer, ReadOnlyMemory<char> value, char delimiter, CancellationToken cancellationToken)
    {
        if (!NeedsEscaping(value.Span, delimiter))
        {
            await writer.WriteAsync(value).ConfigureAwait(false);
            return;
        }

        await writer.WriteAsync("\"").ConfigureAwait(false);
        var start = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var quoteIndex = value.Span[start..].IndexOf('"');
            if (quoteIndex < 0)
            {
                await writer.WriteAsync(value[start..]).ConfigureAwait(false);
                break;
            }

            var absoluteQuoteIndex = start + quoteIndex;
            await writer.WriteAsync(value[start..absoluteQuoteIndex]).ConfigureAwait(false);
            await writer.WriteAsync("\"\"").ConfigureAwait(false);
            start = absoluteQuoteIndex + 1;
        }

        await writer.WriteAsync("\"").ConfigureAwait(false);
    }

    private static void WriteNewLine(TextWriter writer, string newLine)
    {
        writer.Write(newLine);
    }

    private static ValueTask WriteNewLineAsync(TextWriter writer, string newLine)
    {
        return writer.WriteAsync(newLine.AsMemory());
    }

    private static bool NeedsEscaping(ReadOnlySpan<char> value, char delimiter)
    {
        return value.IndexOfAny(delimiter, '"', '\r', '\n') >= 0;
    }
}
