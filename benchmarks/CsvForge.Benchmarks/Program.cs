using System.Dynamic;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CsvForge;

BenchmarkRunner.Run<CsvSerializationBenchmarks>();
BenchmarkRunner.Run<DynamicCsvSerializationBenchmarks>();

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
