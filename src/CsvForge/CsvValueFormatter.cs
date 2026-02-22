using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CsvForge;

internal static class CsvValueFormatter
{
    public static void WriteField<T>(TextWriter writer, T value, CsvSerializationContext context)
    {
        if (value is null)
        {
            return;
        }

        if (value is string text)
        {
            WriteEscapedSpan(writer, text.AsSpan(), context.Options.Delimiter, context);
            return;
        }

        var charsWritten = TryFormatValueToBuffer(value, context.Options.FormatProvider, context);
        WriteEscapedSpan(writer, context.FormatBuffer.AsSpan(0, charsWritten), context.Options.Delimiter, context);
    }

    public static ValueTask WriteFieldAsync<T>(TextWriter writer, T value, CsvSerializationContext context, CancellationToken cancellationToken)
    {
        if (value is null)
        {
            return ValueTask.CompletedTask;
        }

        if (value is string text)
        {
            return WriteEscapedMemoryAsync(writer, text.AsMemory(), context.Options.Delimiter, cancellationToken, context);
        }

        var charsWritten = TryFormatValueToBuffer(value, context.Options.FormatProvider, context);
        return WriteEscapedMemoryAsync(writer, context.FormatBuffer.AsMemory(0, charsWritten), context.Options.Delimiter, cancellationToken, context);
    }

    private static int TryFormatValueToBuffer<T>(T value, IFormatProvider formatProvider, CsvSerializationContext context)
    {
        if (value is ISpanFormattable spanFormattable)
        {
            while (true)
            {
                if (spanFormattable.TryFormat(context.FormatBuffer, out var charsWritten, default, formatProvider))
                {
                    return charsWritten;
                }

                GrowBuffer(context, context.FormatBuffer.Length * 2);
            }
        }

        if (value is IFormattable formattable)
        {
            return CopyStringToBuffer(formattable.ToString(null, formatProvider) ?? string.Empty, context);
        }

        return CopyStringToBuffer(value?.ToString() ?? string.Empty, context);
    }

    private static int CopyStringToBuffer(string value, CsvSerializationContext context)
    {
        EnsureCapacity(context, value.Length);
        value.AsSpan().CopyTo(context.FormatBuffer);
        return value.Length;
    }

    private static void EnsureCapacity(CsvSerializationContext context, int requiredLength)
    {
        if (requiredLength <= context.FormatBuffer.Length)
        {
            return;
        }

        GrowBuffer(context, requiredLength);
    }

    private static void GrowBuffer(CsvSerializationContext context, int targetSize)
    {
        var newBuffer = ArrayPool<char>.Shared.Rent(targetSize);
        ArrayPool<char>.Shared.Return(context.FormatBuffer);
        context.FormatBuffer = newBuffer;
    }

    private static void WriteEscapedSpan(TextWriter writer, ReadOnlySpan<char> value, char delimiter, CsvSerializationContext context)
    {
        if (!NeedsEscaping(value, delimiter))
        {
            writer.Write(value);
            return;
        }

        using var pooledBuilder = context.RentStringBuilder();
        var builder = pooledBuilder.Value;
        builder.Append('"');

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (current == '"')
            {
                builder.Append("\"\"");
            }
            else
            {
                builder.Append(current);
            }
        }

        builder.Append('"');
        writer.Write(builder.ToString());
    }

    private static async ValueTask WriteEscapedMemoryAsync(TextWriter writer, ReadOnlyMemory<char> value, char delimiter, CancellationToken cancellationToken, CsvSerializationContext context)
    {
        if (!NeedsEscaping(value.Span, delimiter))
        {
            await writer.WriteAsync(value).ConfigureAwait(false);
            return;
        }

        using var pooledBuilder = context.RentStringBuilder();
        var builder = pooledBuilder.Value;
        builder.Append('"');

        var span = value.Span;
        for (var i = 0; i < span.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = span[i];
            if (current == '"')
            {
                builder.Append("\"\"");
            }
            else
            {
                builder.Append(current);
            }
        }

        builder.Append('"');
        await writer.WriteAsync(builder.ToString().AsMemory()).ConfigureAwait(false);
    }

    private static bool NeedsEscaping(ReadOnlySpan<char> value, char delimiter)
    {
        return value.IndexOfAny(delimiter, '"', '\r', '\n') >= 0;
    }
}
