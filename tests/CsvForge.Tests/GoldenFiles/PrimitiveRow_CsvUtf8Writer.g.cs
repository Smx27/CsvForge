using System;
using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Demo;

file sealed class PrimitiveRow_CsvUtf8Writer : global::CsvForge.ICsvUtf8TypeWriter<global::Demo.PrimitiveRow>
{
    public static readonly PrimitiveRow_CsvUtf8Writer Instance = new();
    private static readonly byte[][] HeaderColumnsUtf8 = new[]
    {
        Encoding.UTF8.GetBytes("Id"),
        Encoding.UTF8.GetBytes("Count"),
        Encoding.UTF8.GetBytes("Ratio"),
        Encoding.UTF8.GetBytes("Amount"),
        Encoding.UTF8.GetBytes("CreatedAt"),
        Encoding.UTF8.GetBytes("CorrelationId"),
        Encoding.UTF8.GetBytes("IsActive"),
        Encoding.UTF8.GetBytes("OptionalCount"),
    };

    public void WriteHeader(IBufferWriter<byte> writer, global::CsvForge.CsvOptions options)
    {
        for (var i = 0; i < HeaderColumnsUtf8.Length; i++)
        {
            if (i > 0)
            {
                var delimiter = writer.GetSpan(1);
                delimiter[0] = (byte)options.Delimiter;
                writer.Advance(1);
            }
            global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, HeaderColumnsUtf8[i], (byte)options.Delimiter);
        }
    }

    public void WriteRow(IBufferWriter<byte> writer, global::Demo.PrimitiveRow value, global::CsvForge.CsvOptions options)
    {
        {
            Span<byte> utf8Formatted = stackalloc byte[32];
            if (!Utf8Formatter.TryFormat(value.Id, utf8Formatted, out var bytesWritten, 'D'))
            {
                throw new InvalidOperationException("Could not format value as UTF-8.");
            }
            global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, utf8Formatted.Slice(0, bytesWritten), (byte)options.Delimiter);
        }
        {
            var delimiter = writer.GetSpan(1);
            delimiter[0] = (byte)options.Delimiter;
            writer.Advance(1);
        }
        {
            Span<byte> utf8Formatted = stackalloc byte[32];
            if (!Utf8Formatter.TryFormat(value.Count, utf8Formatted, out var bytesWritten, 'D'))
            {
                throw new InvalidOperationException("Could not format value as UTF-8.");
            }
            global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, utf8Formatted.Slice(0, bytesWritten), (byte)options.Delimiter);
        }
        {
            var delimiter = writer.GetSpan(1);
            delimiter[0] = (byte)options.Delimiter;
            writer.Advance(1);
        }
        {
            Span<byte> utf8Formatted = stackalloc byte[64];
            if (!Utf8Formatter.TryFormat(value.Ratio, utf8Formatted, out var bytesWritten, 'G'))
            {
                throw new InvalidOperationException("Could not format value as UTF-8.");
            }
            global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, utf8Formatted.Slice(0, bytesWritten), (byte)options.Delimiter);
        }
        {
            var delimiter = writer.GetSpan(1);
            delimiter[0] = (byte)options.Delimiter;
            writer.Advance(1);
        }
        {
            Span<byte> utf8Formatted = stackalloc byte[64];
            if (!Utf8Formatter.TryFormat(value.Amount, utf8Formatted, out var bytesWritten, 'G'))
            {
                throw new InvalidOperationException("Could not format value as UTF-8.");
            }
            global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, utf8Formatted.Slice(0, bytesWritten), (byte)options.Delimiter);
        }
        {
            var delimiter = writer.GetSpan(1);
            delimiter[0] = (byte)options.Delimiter;
            writer.Advance(1);
        }
        {
            Span<byte> utf8Formatted = stackalloc byte[128];
            if (!Utf8Formatter.TryFormat(value.CreatedAt, utf8Formatted, out var bytesWritten, 'O'))
            {
                throw new InvalidOperationException("Could not format value as UTF-8.");
            }
            global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, utf8Formatted.Slice(0, bytesWritten), (byte)options.Delimiter);
        }
        {
            var delimiter = writer.GetSpan(1);
            delimiter[0] = (byte)options.Delimiter;
            writer.Advance(1);
        }
        {
            Span<byte> utf8Formatted = stackalloc byte[36];
            if (!Utf8Formatter.TryFormat(value.CorrelationId, utf8Formatted, out var bytesWritten, 'D'))
            {
                throw new InvalidOperationException("Could not format value as UTF-8.");
            }
            global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, utf8Formatted.Slice(0, bytesWritten), (byte)options.Delimiter);
        }
        {
            var delimiter = writer.GetSpan(1);
            delimiter[0] = (byte)options.Delimiter;
            writer.Advance(1);
        }
        {
            Span<byte> utf8Formatted = stackalloc byte[8];
            if (!Utf8Formatter.TryFormat(value.IsActive, utf8Formatted, out var bytesWritten))
            {
                throw new InvalidOperationException("Could not format value as UTF-8.");
            }
            global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, utf8Formatted.Slice(0, bytesWritten), (byte)options.Delimiter);
        }
        {
            var delimiter = writer.GetSpan(1);
            delimiter[0] = (byte)options.Delimiter;
            writer.Advance(1);
        }
        var OptionalCountValue = value.OptionalCount;
        if (OptionalCountValue.HasValue)
        {
            {
                Span<byte> utf8Formatted = stackalloc byte[32];
                if (!Utf8Formatter.TryFormat(OptionalCountValue.GetValueOrDefault(), utf8Formatted, out var bytesWritten, 'D'))
                {
                    throw new InvalidOperationException("Could not format value as UTF-8.");
                }
                global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, utf8Formatted.Slice(0, bytesWritten), (byte)options.Delimiter);
            }
        }
    }

    public ValueTask WriteHeaderAsync(IBufferWriter<byte> writer, global::CsvForge.CsvOptions options, CancellationToken cancellationToken)
    {
        WriteHeader(writer, options);
        return ValueTask.CompletedTask;
    }

    public ValueTask WriteRowAsync(IBufferWriter<byte> writer, global::Demo.PrimitiveRow value, global::CsvForge.CsvOptions options, CancellationToken cancellationToken)
    {
        WriteRow(writer, value, options);
        return ValueTask.CompletedTask;
    }
}
