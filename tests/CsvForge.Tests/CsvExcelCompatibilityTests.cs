using System.Globalization;
using System.Text;
using CsvForge;

namespace CsvForge.Tests;

public class CsvExcelCompatibilityTests
{
    [Fact]
    public void ExcelMode_StreamTarget_ShouldEmitUtf8Bom()
    {
        var rows = new[] { new ExcelRow { Value = "a" } };
        using var stream = new MemoryStream();

        CsvWriter.Write(rows, stream, new CsvOptions
        {
            ExcelCompatibility = true,
            EnableRuntimeMetadataFallback = true
        });

        var bytes = stream.ToArray();
        Assert.True(bytes.Length >= 3);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes.Take(3).ToArray());
    }

    [Fact]
    public void DefaultMode_StreamTarget_ShouldNotEmitUtf8Bom()
    {
        var rows = new[] { new ExcelRow { Value = "a" } };
        using var stream = new MemoryStream();

        CsvWriter.Write(rows, stream, new CsvOptions
        {
            EnableRuntimeMetadataFallback = true
        });

        var bytes = stream.ToArray();
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);
    }

    [Fact]
    public void ExcelMode_ShouldEnforceCrLfRowTerminators()
    {
        var rows = new[] { new ExcelRow { Value = "x" } };
        using var writer = new StringWriter();

        CsvWriter.Write(rows, writer, new CsvOptions
        {
            ExcelCompatibility = true,
            EnableRuntimeMetadataFallback = true
        });

        var output = writer.ToString();
        Assert.Contains("\r\n", output, StringComparison.Ordinal);
        Assert.DoesNotContain("Value\nx", output, StringComparison.Ordinal);
    }

    [Fact]
    public void ExcelMode_ShouldFallbackDelimiterForDecimalCommaCulture()
    {
        var rows = new[] { new ExcelPairRow { Left = "a", Right = "b" } };
        using var writer = new StringWriter();

        CsvWriter.Write(rows, writer, new CsvOptions
        {
            ExcelCompatibility = true,
            FormatProvider = new CultureInfo("fr-FR"),
            EnableRuntimeMetadataFallback = true
        });

        var output = writer.ToString();
        Assert.StartsWith("Left;Right", output, StringComparison.Ordinal);
    }

    [Fact]
    public void ExcelMode_ShouldPreserveQuotes_AndNormalizeEmbeddedNewlinesToCrLf()
    {
        var rows = new[]
        {
            new ExcelRow { Value = "say \"hello\"\nline2" }
        };

        using var writer = new StringWriter();
        CsvWriter.Write(rows, writer, new CsvOptions
        {
            ExcelCompatibility = true,
            ExplicitNewLine = "\r\n",
            EnableRuntimeMetadataFallback = true
        });

        var output = writer.ToString();
        Assert.Contains("\"say \"\"hello\"\"\r\nline2\"", output, StringComparison.Ordinal);
    }

    [Fact]
    public void ExcelMode_StreamTarget_ShouldEmitBomOnlyOnceAcrossCheckpointBatches()
    {
        var rows = Enumerable.Range(1, 4).Select(i => new ExcelRow { Value = $"v{i}" }).ToAsyncEnumerable();
        var path = Path.Combine(Path.GetTempPath(), $"csvforge-excel-bom-{Guid.NewGuid():N}.csv");
        var checkpointPath = Path.Combine(Path.GetTempPath(), $"csvforge-excel-bom-{Guid.NewGuid():N}.ckpt");

        try
        {
            CsvWriter.WriteWithCheckpointAsync(rows, path, new CsvCheckpointOptions
            {
                BatchSize = 2,
                CheckpointFilePath = checkpointPath,
                CsvOptions = new CsvOptions
                {
                    ExcelCompatibility = true,
                    EnableRuntimeMetadataFallback = true
                }
            }).GetAwaiter().GetResult();

            var bytes = File.ReadAllBytes(path);
            var bomCount = CountBom(bytes);
            Assert.Equal(1, bomCount);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(checkpointPath)) File.Delete(checkpointPath);
        }
    }

    private static int CountBom(byte[] bytes)
    {
        var count = 0;
        for (var i = 0; i <= bytes.Length - 3; i++)
        {
            if (bytes[i] == 0xEF && bytes[i + 1] == 0xBB && bytes[i + 2] == 0xBF)
            {
                count++;
            }
        }

        return count;
    }

    private sealed class ExcelRow
    {
        public string? Value { get; set; }
    }

    private sealed class ExcelPairRow
    {
        public string? Left { get; set; }

        public string? Right { get; set; }
    }
}
