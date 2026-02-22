using System.Text.Json.Serialization;
using CsvForge;
using CsvForge.Attributes;

namespace CsvForge.Tests;

public class CsvColumnMetadataTests
{
    [Fact]
    public void Write_ShouldUseCsvColumnNameBeforeJsonPropertyName()
    {
        var records = new[] { new NamedRecord { Value = 42 } };
        using var writer = new StringWriter();

        CsvWriter.Write(records, writer, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf });

        var output = writer.ToString();
        Assert.StartsWith("csv_name\n", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_ShouldOrderColumnsByCsvOrderThenDeclaration()
    {
        var records = new[] { new OrderedRecord { B = 2, C = 3, A = 1 } };
        using var writer = new StringWriter();

        CsvWriter.Write(records, writer, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf });

        var lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("second,first,third", lines[0]);
        Assert.Equal("2,1,3", lines[1]);
    }

    private sealed class NamedRecord
    {
        [CsvColumn("csv_name")]
        [JsonPropertyName("json_name")]
        public int Value { get; set; }
    }

    private sealed class OrderedRecord
    {
        [CsvColumn("third")]
        public int C { get; set; }

        [CsvColumn("second", Order = 0)]
        public int B { get; set; }

        [CsvColumn("first", Order = 1)]
        public int A { get; set; }
    }
}
