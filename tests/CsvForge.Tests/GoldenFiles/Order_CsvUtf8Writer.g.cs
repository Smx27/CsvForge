using System;
using System.Buffers;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Demo;

file sealed class Order_CsvUtf8Writer : global::CsvForge.ICsvUtf8TypeWriter<global::Demo.Order>
{
    public static readonly Order_CsvUtf8Writer Instance = new();
    private static readonly byte[][] HeaderColumnsUtf8 = new[]
    {
        Encoding.UTF8.GetBytes("id"),
        Encoding.UTF8.GetBytes("CustomerName"),
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

    public void WriteRow(IBufferWriter<byte> writer, global::Demo.Order value, global::CsvForge.CsvOptions options)
    {
        Span<char> formatted = stackalloc char[64];
        value.Id.TryFormat(formatted, out var charsWritten, default, CultureInfo.InvariantCulture);
        global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, formatted.Slice(0, charsWritten), (byte)options.Delimiter);
        var delimiter = writer.GetSpan(1);
        delimiter[0] = (byte)options.Delimiter;
        writer.Advance(1);
        global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, value.CustomerName?.ToString(), (byte)options.Delimiter);
    }

    public ValueTask WriteHeaderAsync(IBufferWriter<byte> writer, global::CsvForge.CsvOptions options, CancellationToken cancellationToken)
    {
        WriteHeader(writer, options);
        return ValueTask.CompletedTask;
    }

    public ValueTask WriteRowAsync(IBufferWriter<byte> writer, global::Demo.Order value, global::CsvForge.CsvOptions options, CancellationToken cancellationToken)
    {
        WriteRow(writer, value, options);
        return ValueTask.CompletedTask;
    }
}
