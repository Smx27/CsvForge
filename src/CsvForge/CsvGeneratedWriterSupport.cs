using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CsvForge;

public static class CsvGeneratedWriterSupport
{
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
}
