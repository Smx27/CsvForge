using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Buffers;
using System.IO.Pipelines;
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

        var output = SerializeSync(rows, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true });
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

        var output = SerializeSync(rows, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true });
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
            NewLineBehavior = CsvNewLineBehavior.Lf,
            EnableRuntimeMetadataFallback = true
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

        var withHeader = SerializeSync(rows, new CsvOptions { IncludeHeader = true, NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true });
        var withoutHeader = SerializeSync(rows, new CsvOptions { IncludeHeader = false, NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true });

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

        var output = SerializeSync(rows, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true });
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

        var options = new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true };

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

        var options = new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true };

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

        var options = new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true };

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

    [Fact]
    public void StrictMode_ShouldRejectReflectionFallbackEvenWhenExplicitlyEnabled()
    {
        var rows = new[] { new FallbackOptInRow { Value = 42 } };
        using var writer = new StringWriter();

        var exception = Assert.Throws<InvalidOperationException>(() => CsvWriter.Write(rows, writer, new CsvOptions
        {
            NewLineBehavior = CsvNewLineBehavior.Lf,
            EnableRuntimeMetadataFallback = true,
            StrictMode = true
        }));

        Assert.Contains("StrictMode", exception.Message, StringComparison.Ordinal);
        Assert.Contains("[CsvSerializable]", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NonStrictMode_ShouldPreserveReflectionFallbackBehavior()
    {
        var rows = new[] { new FallbackOptInRow { Value = 11 } };
        using var writer = new StringWriter();

        CsvWriter.Write(rows, writer, new CsvOptions
        {
            NewLineBehavior = CsvNewLineBehavior.Lf,
            EnableRuntimeMetadataFallback = true,
            StrictMode = false
        });

        Assert.Equal("Value\n11\n", writer.ToString());
    }

    [Fact]
    public void Write_ShouldRequireGeneratedWriterOrExplicitRuntimeFallbackOptIn()
    {
        var rows = new[] { new FallbackOptInRow { Value = 10 } };
        using var writer = new StringWriter();

        var exception = Assert.Throws<InvalidOperationException>(() => CsvWriter.Write(rows, writer, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf }));
        Assert.Contains("EnableRuntimeMetadataFallback", exception.Message, StringComparison.Ordinal);
    }


    [Fact]
    public async Task WriteToFileAndWriteToFileAsync_ShouldWriteExpectedCsv()
    {
        var rows = new[] { new DelimitedRow { A = "x", B = "y" } };
        var options = new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true };

        var syncPath = Path.Combine(Path.GetTempPath(), $"csvforge-sync-{Guid.NewGuid():N}.csv");
        var asyncPath = Path.Combine(Path.GetTempPath(), $"csvforge-async-{Guid.NewGuid():N}.csv");

        try
        {
            CsvWriter.WriteToFile(rows, syncPath, options);
            await CsvWriter.WriteToFileAsync(rows, asyncPath, options);

            Assert.Equal("A,B\nx,y\n", File.ReadAllText(syncPath));
            Assert.Equal("A,B\nx,y\n", File.ReadAllText(asyncPath));
        }
        finally
        {
            if (File.Exists(syncPath))
            {
                File.Delete(syncPath);
            }

            if (File.Exists(asyncPath))
            {
                File.Delete(asyncPath);
            }
        }
    }

    [Fact]
    public void Write_ShouldUseRegisteredGeneratedWriterWithoutRuntimeFallback()
    {
        CsvTypeWriterCache<GeneratedPathRow>.Register(GeneratedPathRowWriter.Instance);
        var rows = new[] { new GeneratedPathRow { Value = 7 } };
        using var writer = new StringWriter();

        CsvWriter.Write(rows, writer, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf });

        Assert.Equal("value\n7\n", writer.ToString());
    }

    [Fact]
    public void RuntimeFallbackPath_ShouldBeAnnotatedWithRequiresUnreferencedCode()
    {
        var serializerType = typeof(CsvWriter).Assembly.GetType("CsvForge.CsvSerializer", throwOnError: true)!;
        var method = serializerType.GetMethod("CreateGeneratedWriterRequiredException", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var attribute = method!.GetCustomAttribute<RequiresUnreferencedCodeAttribute>();
        Assert.NotNull(attribute);
        Assert.Contains("not trimming-safe", attribute!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Write_ShouldSupportBufferWriterAndPipeWriterTargets()
    {
        var rows = new[] { new DelimitedRow { A = "x", B = "y" } };
        var options = new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true };

        var arrayBuffer = new ArrayBufferWriter<byte>();
        CsvWriter.Write(rows, arrayBuffer, options);
        var fromBufferWriter = options.Encoding.GetString(arrayBuffer.WrittenSpan);

        var pipe = new Pipe();
        CsvWriter.Write(rows, pipe.Writer, options);
        var result = pipe.Reader.ReadAsync().GetAwaiter().GetResult();
        var fromPipeWriter = options.Encoding.GetString(result.Buffer.ToArray());
        pipe.Reader.AdvanceTo(result.Buffer.End);

        Assert.Equal("A,B\nx,y\n", fromBufferWriter);
        Assert.Equal("A,B\nx,y\n", fromPipeWriter);
    }

    [Fact]
    public async Task WriteAsync_ShouldSupportBufferWriterAndPipeWriterTargets()
    {
        var rows = new[] { new DelimitedRow { A = "x", B = "y" } };
        var options = new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf, EnableRuntimeMetadataFallback = true };

        var arrayBuffer = new ArrayBufferWriter<byte>();
        await CsvWriter.WriteAsync(rows, arrayBuffer, options);
        var fromBufferWriter = options.Encoding.GetString(arrayBuffer.WrittenSpan);

        var pipe = new Pipe();
        await CsvWriter.WriteAsync(rows, pipe.Writer, options);
        var result = await pipe.Reader.ReadAsync();
        var fromPipeWriter = options.Encoding.GetString(result.Buffer.ToArray());
        pipe.Reader.AdvanceTo(result.Buffer.End);

        Assert.Equal("A,B\nx,y\n", fromBufferWriter);
        Assert.Equal("A,B\nx,y\n", fromPipeWriter);
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

    private sealed class FallbackOptInRow
    {
        public int Value { get; set; }
    }

    private sealed class GeneratedPathRow
    {
        public int Value { get; set; }
    }

    private sealed class GeneratedPathRowWriter : ICsvTypeWriter<GeneratedPathRow>
    {
        public static GeneratedPathRowWriter Instance { get; } = new();

        public void WriteHeader(TextWriter writer, CsvOptions options)
        {
            writer.Write("value");
        }

        public void WriteRow(TextWriter writer, GeneratedPathRow value, CsvOptions options)
        {
            writer.Write(value.Value);
        }

        public ValueTask WriteHeaderAsync(TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
        {
            WriteHeader(writer, options);
            return ValueTask.CompletedTask;
        }

        public ValueTask WriteRowAsync(TextWriter writer, GeneratedPathRow value, CsvOptions options, CancellationToken cancellationToken)
        {
            WriteRow(writer, value, options);
            return ValueTask.CompletedTask;
        }
    }
}
