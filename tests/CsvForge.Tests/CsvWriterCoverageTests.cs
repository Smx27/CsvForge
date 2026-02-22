using System.Diagnostics;
using System.Text.Json.Serialization;
using CsvForge;
using CsvForge.Attributes;

namespace CsvForge.Tests;

public class CsvWriterCoverageTests
{
    [Fact]
    public void Write_ShouldHandleNullsAndNullableTypes()
    {
        var rows = new[]
        {
            new NullableRow
            {
                Id = 1,
                OptionalInt = null,
                OptionalDate = null,
                OptionalText = null,
                RequiredText = "value"
            },
            new NullableRow
            {
                Id = 2,
                OptionalInt = 9,
                OptionalDate = new DateTime(2025, 01, 02, 03, 04, 05, DateTimeKind.Utc),
                OptionalText = "present",
                RequiredText = "value2"
            }
        };

        var output = SerializeSync(rows, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf });
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Id,OptionalInt,OptionalDate,OptionalText,RequiredText", lines[0]);
        Assert.Equal("1,,,,value", lines[1]);
        Assert.Equal("2,9,2025-01-02T03:04:05.0000000Z,present,value2", lines[2]);
    }

    [Fact]
    public void Write_ShouldEscapeQuoteCommaAndNewlineCharacters()
    {
        var rows = new[]
        {
            new EscapedRow
            {
                Text = "a,b",
                Quote = "say \"hello\"",
                NewLine = "line1\nline2"
            }
        };

        var output = SerializeSync(rows, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf });
        Assert.StartsWith("Text,Quote,NewLine\n", output, StringComparison.Ordinal);
        Assert.Contains("\"a,b\"", output, StringComparison.Ordinal);
        Assert.Contains("\"say \"\"hello\"\"\"", output, StringComparison.Ordinal);
        Assert.Contains("\"line1\nline2\"", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(',')]
    [InlineData(';')]
    [InlineData('|')]
    [InlineData('\t')]
    [InlineData('#')]
    public void Write_ShouldSupportCustomDelimiters(char delimiter)
    {
        var rows = new[] { new DelimitedRow { A = "x", B = "y" } };

        var output = SerializeSync(rows, new CsvOptions
        {
            Delimiter = delimiter,
            NewLineBehavior = CsvNewLineBehavior.Lf
        });

        var expectedSeparator = delimiter.ToString();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal($"A{expectedSeparator}B", lines[0]);
        Assert.Equal($"x{expectedSeparator}y", lines[1]);
    }

    [Fact]
    public void Write_ShouldRespectIncludeHeaderSetting()
    {
        var rows = new[] { new DelimitedRow { A = "x", B = "y" } };

        var withHeader = SerializeSync(rows, new CsvOptions { IncludeHeader = true, NewLineBehavior = CsvNewLineBehavior.Lf });
        var withoutHeader = SerializeSync(rows, new CsvOptions { IncludeHeader = false, NewLineBehavior = CsvNewLineBehavior.Lf });

        Assert.StartsWith("A,B\n", withHeader, StringComparison.Ordinal);
        Assert.Equal("x,y\n", withoutHeader);
    }

    [Fact]
    public void Write_ShouldUseCsvColumnNameThenJsonPropertyNameAndOrdering()
    {
        var rows = new[]
        {
            new AttributeRow
            {
                OrderedFirst = 1,
                OrderedSecond = 2,
                JsonNamed = 3,
                CsvNamed = 4,
                Plain = 5
            }
        };

        var output = SerializeSync(rows, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf });
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("first,second,json_name,csv_name,Plain", lines[0]);
        Assert.Equal("1,2,3,4,5", lines[1]);
    }

    [Fact]
    public async Task WriteAndWriteAsync_ShouldProduceEquivalentOutput_ForEnumerableInputs()
    {
        IEnumerable<DelimitedRow> enumerable = new List<DelimitedRow>
        {
            new() { A = "1", B = "2" },
            new() { A = "3", B = "4" }
        };

        var options = new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf };

        var syncOutput = SerializeSync(enumerable, options);
        var asyncOutput = await SerializeAsync(enumerable, options);

        Assert.Equal(syncOutput, asyncOutput);
    }

    [Fact]
    public async Task WriteAndWriteAsync_ShouldSupportListArrayEnumerableAndAsyncEnumerable()
    {
        var list = new List<DelimitedRow>
        {
            new() { A = "a", B = "b" },
            new() { A = "c", B = "d" }
        };
        var array = list.ToArray();
        IEnumerable<DelimitedRow> enumerable = list;

        var options = new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf };

        var listOutput = SerializeSync(list, options);
        var arrayOutput = SerializeSync(array, options);
        var enumerableOutput = SerializeSync(enumerable, options);
        var asyncEnumerableOutput = await SerializeAsync(ToAsyncEnumerable(list), options);

        Assert.Equal(listOutput, arrayOutput);
        Assert.Equal(listOutput, enumerableOutput);
        Assert.Equal(listOutput, asyncEnumerableOutput);
    }

    [Fact]
    public async Task WriteAndWriteAsync_LargeDataset_ShouldNotCorruptRows_AndCompleteWithinEnvelope()
    {
        const int rowCount = 100_000;
        var rows = Enumerable.Range(1, rowCount)
            .Select(static index => new LargeRow
            {
                Id = index,
                Name = $"row-{index}",
                Note = index % 2 == 0 ? "even" : "odd"
            })
            .ToArray();

        var options = new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf };

        var syncWatch = Stopwatch.StartNew();
        var syncOutput = SerializeSync(rows, options);
        syncWatch.Stop();

        var asyncWatch = Stopwatch.StartNew();
        var asyncOutput = await SerializeAsync(rows, options);
        asyncWatch.Stop();

        Assert.Equal(syncOutput, asyncOutput);

        var lines = syncOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(rowCount + 1, lines.Length);
        Assert.Equal("Id,Name,Note", lines[0]);
        Assert.Equal("1,row-1,odd", lines[1]);
        Assert.Equal($"{rowCount},row-{rowCount},even", lines[^1]);

        Assert.DoesNotContain("\r", syncOutput, StringComparison.Ordinal);
        Assert.Equal(rowCount + 1, syncOutput.Count(static c => c == '\n'));

        Assert.InRange(syncWatch.Elapsed.TotalSeconds, 0, 30);
        Assert.InRange(asyncWatch.Elapsed.TotalSeconds, 0, 30);
    }

    private static string SerializeSync<T>(IEnumerable<T> rows, CsvOptions options)
    {
        using var writer = new StringWriter();
        CsvWriter.Write(rows, writer, options);
        return writer.ToString();
    }

    private static async Task<string> SerializeAsync<T>(IEnumerable<T> rows, CsvOptions options)
    {
        using var writer = new StringWriter();
        await CsvWriter.WriteAsync(rows, writer, options);
        return writer.ToString();
    }

    private static async Task<string> SerializeAsync<T>(IAsyncEnumerable<T> rows, CsvOptions options)
    {
        using var writer = new StringWriter();
        await CsvWriter.WriteAsync(rows, writer, options);
        return writer.ToString();
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            await Task.Yield();
            yield return item;
        }
    }

    private sealed class NullableRow
    {
        public int Id { get; set; }

        public int? OptionalInt { get; set; }

        public DateTime? OptionalDate { get; set; }

        public string? OptionalText { get; set; }

        public string RequiredText { get; set; } = string.Empty;
    }

    private sealed class EscapedRow
    {
        public string Text { get; set; } = string.Empty;

        public string Quote { get; set; } = string.Empty;

        public string NewLine { get; set; } = string.Empty;
    }

    private sealed class DelimitedRow
    {
        public string A { get; set; } = string.Empty;

        public string B { get; set; } = string.Empty;
    }

    private sealed class AttributeRow
    {
        [CsvColumn("first", Order = 0)]
        public int OrderedFirst { get; set; }

        [CsvColumn("second", Order = 1)]
        public int OrderedSecond { get; set; }

        [JsonPropertyName("json_name")]
        public int JsonNamed { get; set; }

        [CsvColumn("csv_name")]
        [JsonPropertyName("ignored_json_name")]
        public int CsvNamed { get; set; }

        public int Plain { get; set; }
    }

    private sealed class LargeRow
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;
    }
}
