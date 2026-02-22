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

        CsvWriter.Write(records, writer, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true });

        var output = writer.ToString();
        Assert.StartsWith("csv_name\n", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_ShouldUseJsonPropertyNameWhenCsvColumnNameIsMissing()
    {
        var records = new[] { new JsonNamedRecord { Value = 42 } };
        using var writer = new StringWriter();

        CsvWriter.Write(records, writer, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true });

        var output = writer.ToString();
        Assert.StartsWith("json_name\n", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_ShouldIgnoreCsvIgnoredProperties()
    {
        var records = new[] { new IgnoredRecord { Included = 1, Ignored = 2 } };
        using var writer = new StringWriter();

        CsvWriter.Write(records, writer, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true });

        var lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("included", lines[0]);
        Assert.Equal("1", lines[1]);
    }

    [Fact]
    public void Write_ShouldOrderColumnsByCsvOrderThenDeclarationThenPropertyName()
    {
        var records = new[] { new OrderedRecord { B = 2, C = 3, A = 1, Z = 26, M = 13 } };
        using var writer = new StringWriter();

        CsvWriter.Write(records, writer, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true });

        var lines = writer.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("second,alpha,omega,first,third", lines[0]);
        Assert.Equal("2,13,26,1,3", lines[1]);
    }

    private sealed class NamedRecord
    {
        [CsvColumn("csv_name")]
        [JsonPropertyName("json_name")]
        public int Value { get; set; }
    }

    private sealed class JsonNamedRecord
    {
        [JsonPropertyName("json_name")]
        public int Value { get; set; }
    }

    private sealed class IgnoredRecord
    {
        [CsvColumn("included")]
        public int Included { get; set; }

        [CsvIgnore]
        [CsvColumn("ignored")]
        public int Ignored { get; set; }
    }

    private sealed class OrderedRecord
    {
        [CsvColumn("third")]
        public int C { get; set; }

        [CsvColumn("second", Order = 0)]
        public int B { get; set; }

        [CsvColumn("first", Order = 1)]
        public int A { get; set; }

        [CsvColumn("omega", Order = 1)]
        public int Z { get; set; }

        [CsvColumn("alpha", Order = 1)]
        public int M { get; set; }
    }
}
