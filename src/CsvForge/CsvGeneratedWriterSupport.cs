using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CsvForge;

public static class CsvGeneratedWriterSupport
{
    private static readonly byte QuoteByte = (byte)'"';

    public static void WriteEscaped(TextWriter writer, string? value, char delimiter)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (!NeedsEscaping(value, delimiter))
        {
            writer.Write(value);
            return;
        }

        writer.Write('"');
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (current == '"')
            {
                writer.Write("\"\"");
            }
            else
            {
                writer.Write(current);
            }
        }

        writer.Write('"');
    }

    public static void WriteEscaped(TextWriter writer, ReadOnlySpan<char> value, char delimiter)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (value.IsEmpty)
        {
            return;
        }

        if (!NeedsEscaping(value, delimiter))
        {
            writer.Write(value);
            return;
        }

        writer.Write('"');
        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (current == '"')
            {
                writer.Write("\"\"");
            }
            else
            {
                writer.Write(current);
            }
        }

        writer.Write('"');
    }

    public static async ValueTask WriteEscapedAsync(TextWriter writer, string? value, char delimiter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (!NeedsEscaping(value, delimiter))
        {
            await writer.WriteAsync(value.AsMemory()).ConfigureAwait(false);
            return;
        }

        await writer.WriteAsync("\"").ConfigureAwait(false);
        for (var i = 0; i < value.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = value[i];
            if (current == '"')
            {
                await writer.WriteAsync("\"\"").ConfigureAwait(false);
            }
            else
            {
                await writer.WriteAsync(current).ConfigureAwait(false);
            }
        }

        await writer.WriteAsync("\"").ConfigureAwait(false);
    }

    private static bool NeedsEscaping(string value, char delimiter)
    {
        return value.IndexOfAny(new[] { delimiter, '"', '\r', '\n' }) >= 0;
    }

    private static bool NeedsEscaping(ReadOnlySpan<char> value, char delimiter)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c == delimiter || c == '"' || c == '\r' || c == '\n')
            {
                return true;
            }
        }

        return false;
    }

    public static void WriteEscapedUtf8(IBufferWriter<byte> writer, string? value, byte delimiter)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        WriteEscapedUtf8(writer, value.AsSpan(), delimiter);
    }

    public static void WriteEscapedUtf8(IBufferWriter<byte> writer, ReadOnlySpan<char> value, byte delimiter)
    {
        if (value.IsEmpty)
        {
            return;
        }

        var bytes = Encoding.UTF8.GetBytes(value.ToString());
        WriteEscapedUtf8(writer, bytes, delimiter);
    }

    public static void WriteEscapedUtf8(IBufferWriter<byte> writer, ReadOnlySpan<byte> value, byte delimiter)
    {
        if (value.IsEmpty)
        {
            return;
        }

        if (!NeedsEscaping(value, delimiter))
        {
            var target = writer.GetSpan(value.Length);
            value.CopyTo(target);
            writer.Advance(value.Length);
            return;
        }

        var open = writer.GetSpan(1);
        open[0] = QuoteByte;
        writer.Advance(1);

        for (var i = 0; i < value.Length; i++)
        {
            var current = value[i];
            if (current == QuoteByte)
            {
                var escaped = writer.GetSpan(2);
                escaped[0] = QuoteByte;
                escaped[1] = QuoteByte;
                writer.Advance(2);
            }
            else
            {
                var destination = writer.GetSpan(1);
                destination[0] = current;
                writer.Advance(1);
            }
        }

        var close = writer.GetSpan(1);
        close[0] = QuoteByte;
        writer.Advance(1);
    }

    private static bool NeedsEscaping(ReadOnlySpan<byte> value, byte delimiter)
    {
        for (var i = 0; i < value.Length; i++)
        {
            var b = value[i];
            if (b == delimiter || b == QuoteByte || b == (byte)'\r' || b == (byte)'\n')
            {
                return true;
            }
        }

        return false;
    }
}
