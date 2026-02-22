using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Demo;

file sealed class Order_CsvWriter : global::CsvForge.ICsvTypeWriter<global::Demo.Order>
{
    public static readonly Order_CsvWriter Instance = new();

    public void WriteHeader(TextWriter writer, global::CsvForge.CsvOptions options)
    {
        global::CsvForge.CsvGeneratedWriterSupport.WriteEscaped(writer, "id", options.Delimiter);
        writer.Write(options.Delimiter);
        global::CsvForge.CsvGeneratedWriterSupport.WriteEscaped(writer, "CustomerName", options.Delimiter);
    }

    public void WriteRow(TextWriter writer, global::Demo.Order value, global::CsvForge.CsvOptions options)
    {
        global::CsvForge.CsvGeneratedWriterSupport.WriteEscaped(writer, value.Id.ToString(), options.Delimiter);
        writer.Write(options.Delimiter);
        var CustomerNameValue = value.CustomerName;
        global::CsvForge.CsvGeneratedWriterSupport.WriteEscaped(writer, CustomerNameValue?.ToString(), options.Delimiter);
    }

    public async ValueTask WriteHeaderAsync(TextWriter writer, global::CsvForge.CsvOptions options, CancellationToken cancellationToken)
    {
        await global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedAsync(writer, "id", options.Delimiter, cancellationToken).ConfigureAwait(false);
        await writer.WriteAsync(options.Delimiter).ConfigureAwait(false);
        await global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedAsync(writer, "CustomerName", options.Delimiter, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask WriteRowAsync(TextWriter writer, global::Demo.Order value, global::CsvForge.CsvOptions options, CancellationToken cancellationToken)
    {
        await global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedAsync(writer, value.Id.ToString(), options.Delimiter, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await writer.WriteAsync(options.Delimiter).ConfigureAwait(false);
        var CustomerNameValue = value.CustomerName;
        await global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedAsync(writer, CustomerNameValue?.ToString(), options.Delimiter, cancellationToken).ConfigureAwait(false);
    }
}
