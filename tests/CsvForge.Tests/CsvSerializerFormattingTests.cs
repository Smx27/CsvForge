using System.Globalization;
using CsvForge;

namespace CsvForge.Tests;

public class CsvSerializerFormattingTests
{
    [Fact]
    public void Write_ShouldEscapeQuotedAndDelimitedFields_AndWriteNullAsEmpty()
    {
        var records = new[]
        {
            new Record
            {
                NullableText = null,
                Text = "a,\"b\"",
                Number = 1.5m,
                Date = new DateTime(2024, 01, 02, 03, 04, 05, DateTimeKind.Utc)
            }
        };

        using var writer = new StringWriter();
        CsvWriter.Write(records, writer, new CsvOptions
        {
            NewLineBehavior = CsvNewLineBehavior.Lf,
            FormatProvider = CultureInfo.GetCultureInfo("fr-FR")
        });

        var lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("NullableText,Text,Number,Date", lines[0]);
        Assert.Equal(",\"a,\"\"b\"\"\",1,5,02/01/2024 03:04:05", lines[1]);
    }

    [Fact]
    public async Task WriteAsync_ShouldUseDeterministicNewLinesForHeaderAndRows()
    {
        var records = new[]
        {
            new Record { Text = "x", Number = 1m, Date = new DateTime(2024, 01, 01) },
            new Record { Text = "y", Number = 2m, Date = new DateTime(2024, 01, 02) }
        };

        using var writer = new StringWriter();

        await CsvWriter.WriteAsync(records, writer, new CsvOptions
        {
            NewLineBehavior = CsvNewLineBehavior.CrLf,
            IncludeHeader = true
        });

        var output = writer.ToString();
        Assert.Contains("\r\n", output, StringComparison.Ordinal);
        Assert.DoesNotContain("\n", output.Replace("\r\n", string.Empty), StringComparison.Ordinal);
        Assert.Equal(3, output.Split("\r\n", StringSplitOptions.None).Length - 1);
    }

    private sealed class Record
    {
        public string? NullableText { get; set; }

        public string Text { get; set; } = string.Empty;

        public decimal Number { get; set; }

        public DateTime Date { get; set; }
    }
}
