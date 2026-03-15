using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace Demo;

internal sealed class Order_CsvUtf16Writer : global::CsvForge.ICsvTypeWriter<global::Demo.Order>
{
    public static readonly Order_CsvUtf16Writer Instance = new();
    private static readonly string[] HeaderColumns = new[]
    {
        "id\", Order = 0)] int Id, string? CustomerName);",
    };

    public void WriteHeader(TextWriter writer, global::CsvForge.CsvOptions options)
    {
        for (var i = 0; i < HeaderColumns.Length; i++)
        {
            if (i > 0)
            {
                writer.Write(options.Delimiter);
            }
            global::CsvForge.CsvGeneratedWriterSupport.WriteEscaped(writer, HeaderColumns[i], options.Delimiter);
        }
    }

    public void WriteRow(TextWriter writer, global::Demo.Order value, global::CsvForge.CsvOptions options)
    {
        global::CsvForge.CsvGeneratedWriterSupport.WriteEscaped(writer, value.?.ToString(), options.Delimiter);
    }

    public async ValueTask WriteHeaderAsync(TextWriter writer, global::CsvForge.CsvOptions options, CancellationToken cancellationToken)
    {
        for (var i = 0; i < HeaderColumns.Length; i++)
        {
            if (i > 0)
            {
                await writer.WriteAsync(options.Delimiter).ConfigureAwait(false);
            }
            await global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedAsync(writer, HeaderColumns[i], options.Delimiter, cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask WriteRowAsync(TextWriter writer, global::Demo.Order value, global::CsvForge.CsvOptions options, CancellationToken cancellationToken)
    {
        await global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedAsync(writer, value.?.ToString(), options.Delimiter, cancellationToken).ConfigureAwait(false);
    }
}