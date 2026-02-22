using System.Dynamic;
using System.Reflection;
using System.Buffers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CsvForge;

BenchmarkRunner.Run<CsvSerializationBenchmarks>();
BenchmarkRunner.Run<DynamicCsvSerializationBenchmarks>();
BenchmarkRunner.Run<GeneratorEngineMatrixBenchmarks>();

[MemoryDiagnoser]
public class CsvSerializationBenchmarks
{
    private TestRow[] _rows = Array.Empty<TestRow>();
    private CsvOptions _options = null!;
    private PropertyInfo[] _naiveProperties = Array.Empty<PropertyInfo>();

    [Params(1_000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = new TestRow[RowCount];
        for (var i = 0; i < _rows.Length; i++)
        {
            _rows[i] = new TestRow(i, $"name-{i}", i % 2 == 0 ? "ok" : "quoted \"text\"");
        }

        _options = new CsvOptions
        {
            IncludeHeader = true,
            BufferSize = 64 * 1024,
            StreamWriterBufferSize = 64 * 1024,
            NewLineBehavior = CsvNewLineBehavior.Lf
        };

        _naiveProperties = typeof(TestRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    }

    [Benchmark(Baseline = true)]
    public string NaiveReflectionSync()
    {
        using var writer = new StringWriter();
        WriteNaiveSync(_rows, writer, _naiveProperties);
        return writer.ToString();
    }

    [Benchmark]
    public async Task<string> NaiveReflectionAsync()
    {
        using var writer = new StringWriter();
        await WriteNaiveAsync(_rows, writer, _naiveProperties).ConfigureAwait(false);
        return writer.ToString();
    }

    [Benchmark]
    public string CsvForgeOptimizedSync()
    {
        using var writer = new StringWriter();
        CsvWriter.Write(_rows, writer, _options);
        return writer.ToString();
    }

    [Benchmark]
    public async Task<string> CsvForgeOptimizedAsync()
    {
        using var writer = new StringWriter();
        await CsvWriter.WriteAsync(_rows, writer, _options).ConfigureAwait(false);
        return writer.ToString();
    }

    private static void WriteNaiveSync(IEnumerable<TestRow> rows, TextWriter writer, PropertyInfo[] properties)
    {
        writer.WriteLine(string.Join(",", properties.Select(p => p.Name)));

        foreach (var row in rows)
        {
            var line = string.Empty;

            for (var i = 0; i < properties.Length; i++)
            {
                if (i > 0)
                {
                    line += ",";
                }

                var value = properties[i].GetValue(row)?.ToString() ?? string.Empty;
                line += EscapeCsv(value);
            }

            writer.WriteLine(line);
        }
    }

    private static async Task WriteNaiveAsync(IEnumerable<TestRow> rows, TextWriter writer, PropertyInfo[] properties)
    {
        await writer.WriteLineAsync(string.Join(",", properties.Select(p => p.Name))).ConfigureAwait(false);

        foreach (var row in rows)
        {
            var line = string.Empty;

            for (var i = 0; i < properties.Length; i++)
            {
                if (i > 0)
                {
                    line += ",";
                }

                var value = properties[i].GetValue(row)?.ToString() ?? string.Empty;
                line += EscapeCsv(value);
            }

            await writer.WriteLineAsync(line).ConfigureAwait(false);
        }
    }

    private static string EscapeCsv(string value)
    {
        if (value.IndexOfAny([',', '"', '\r', '\n']) == -1)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    public sealed record TestRow(int Id, string Name, string Notes);
}

[MemoryDiagnoser]
public class DynamicCsvSerializationBenchmarks
{
    private IReadOnlyList<IDictionary<string, object?>> _rows = Array.Empty<IDictionary<string, object?>>();
    private CsvOptions _unionOptions = null!;
    private CsvOptions _firstShapeOptions = null!;

    [Params(100_000, 250_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rows = new List<IDictionary<string, object?>>(RowCount);
        for (var i = 0; i < RowCount; i++)
        {
            IDictionary<string, object?> row = new ExpandoObject();
            row["id"] = i;
            row["name"] = $"name-{i}";

            if (i % 3 == 0)
            {
                row["region"] = $"region-{i % 17}";
            }

            if (i % 5 == 0)
            {
                row["score"] = i * 1.05;
            }

            if (i % 7 == 0)
            {
                row["active"] = true;
            }

            rows.Add(row);
        }

        _rows = rows;
        _unionOptions = new CsvOptions
        {
            IncludeHeader = true,
            NewLineBehavior = CsvNewLineBehavior.Lf,
            HeterogeneousHeaderBehavior = CsvHeterogeneousHeaderBehavior.Union
        };

        _firstShapeOptions = new CsvOptions
        {
            IncludeHeader = true,
            NewLineBehavior = CsvNewLineBehavior.Lf,
            HeterogeneousHeaderBehavior = CsvHeterogeneousHeaderBehavior.FirstShapeLock
        };
    }

    [Benchmark]
    public string Union_Sync()
    {
        using var writer = new StringWriter();
        CsvWriter.Write(_rows, writer, _unionOptions);
        return writer.ToString();
    }

    [Benchmark]
    public string FirstShapeLock_Sync()
    {
        using var writer = new StringWriter();
        CsvWriter.Write(_rows, writer, _firstShapeOptions);
        return writer.ToString();
    }
}


[MemoryDiagnoser]
public class GeneratorEngineMatrixBenchmarks
{
    private SourceGeneratedMatrixRow[] _generatedRows = Array.Empty<SourceGeneratedMatrixRow>();
    private ReflectionFallbackMatrixRow[] _fallbackRows = Array.Empty<ReflectionFallbackMatrixRow>();
    private CsvOptions _generatedOptions = null!;
    private CsvOptions _fallbackOptions = null!;
    private NullBufferWriter _nullBufferWriter = null!;

    [Params(100_000, 1_000_000)]
    public int RowCount { get; set; }

    [Params(CsvDialectScenario.CommaLfNoEscaping, CsvDialectScenario.SemicolonCrLfNoEscaping, CsvDialectScenario.CommaCrLfEscaping)]
    public CsvDialectScenario DialectScenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var (delimiter, newLineBehavior, escapingMode) = DialectScenario.GetSettings();

        _generatedRows = new SourceGeneratedMatrixRow[RowCount];
        _fallbackRows = new ReflectionFallbackMatrixRow[RowCount];

        for (var i = 0; i < RowCount; i++)
        {
            var notes = escapingMode == EscapingMode.Rfc4180Stress
                ? $"note {i},\"escaped\"{(i % 13 == 0 ? "\r\nline-two" : string.Empty)}"
                : $"note-{i}";

            var generated = new SourceGeneratedMatrixRow
            {
                Id = i,
                Name = $"name-{i}",
                IsActive = i % 2 == 0,
                Notes = notes
            };

            _generatedRows[i] = generated;
            _fallbackRows[i] = new ReflectionFallbackMatrixRow
            {
                Id = generated.Id,
                Name = generated.Name,
                IsActive = generated.IsActive,
                Notes = generated.Notes
            };
        }

        _generatedOptions = new CsvOptions
        {
            IncludeHeader = true,
            Delimiter = delimiter,
            NewLineBehavior = newLineBehavior,
            EnableRuntimeMetadataFallback = false
        };

        _fallbackOptions = new CsvOptions
        {
            IncludeHeader = true,
            Delimiter = delimiter,
            NewLineBehavior = newLineBehavior,
            EnableRuntimeMetadataFallback = true
        };

        _nullBufferWriter = new NullBufferWriter();
    }

    [Benchmark(Baseline = true)]
    public void ReflectionFallback_Utf16()
    {
        CsvWriter.Write(_fallbackRows, TextWriter.Null, _fallbackOptions);
    }

    [Benchmark]
    public void SourceGenerated_Utf16()
    {
        CsvWriter.Write(_generatedRows, TextWriter.Null, _generatedOptions);
    }

    [Benchmark]
    public void SourceGenerated_Utf8_Stream()
    {
        CsvWriter.Write(_generatedRows, Stream.Null, _generatedOptions);
    }

    [Benchmark]
    public void SourceGenerated_Utf8_IBufferWriter()
    {
        _nullBufferWriter.Reset();
        CsvWriter.Write(_generatedRows, _nullBufferWriter, _generatedOptions);
    }

    public enum CsvDialectScenario
    {
        CommaLfNoEscaping,
        SemicolonCrLfNoEscaping,
        CommaCrLfEscaping
    }


    private sealed class NullBufferWriter : IBufferWriter<byte>
    {
        private byte[] _buffer = new byte[64 * 1024];

        public void Advance(int count)
        {
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer;
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer;
        }

        public void Reset()
        {
        }

        private void EnsureCapacity(int sizeHint)
        {
            if (sizeHint <= 0 || sizeHint <= _buffer.Length)
            {
                return;
            }

            _buffer = new byte[Math.Max(sizeHint, _buffer.Length * 2)];
        }
    }

    [CsvForge.Attributes.CsvSerializable]
    public partial class SourceGeneratedMatrixRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public string Notes { get; init; } = string.Empty;
    }

    public sealed class ReflectionFallbackMatrixRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public string Notes { get; init; } = string.Empty;
    }

}

public static class CsvDialectScenarioExtensions
{
    public static (char Delimiter, CsvNewLineBehavior NewLine, EscapingMode EscapingMode) GetSettings(this GeneratorEngineMatrixBenchmarks.CsvDialectScenario scenario)
    {
        return scenario switch
        {
            GeneratorEngineMatrixBenchmarks.CsvDialectScenario.CommaLfNoEscaping => (',', CsvNewLineBehavior.Lf, EscapingMode.None),
            GeneratorEngineMatrixBenchmarks.CsvDialectScenario.SemicolonCrLfNoEscaping => (';', CsvNewLineBehavior.CrLf, EscapingMode.None),
            GeneratorEngineMatrixBenchmarks.CsvDialectScenario.CommaCrLfEscaping => (',', CsvNewLineBehavior.CrLf, EscapingMode.Rfc4180Stress),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, null)
        };
    }
}

public enum EscapingMode
{
    None,
    Rfc4180Stress
}
