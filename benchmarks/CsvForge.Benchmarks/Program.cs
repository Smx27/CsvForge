using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CsvForge;

BenchmarkRunner.Run<CsvSerializationBenchmarks>();

[MemoryDiagnoser]
public class CsvSerializationBenchmarks
{
    private TestRow[] _rows = Array.Empty<TestRow>();
    private CsvOptions _options = null!;

    [Params(100_000)]
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
    }

    [Benchmark]
    public string SerializeArraySync()
    {
        using var writer = new StringWriter();
        CsvWriter.Write(_rows, writer, _options);
        return writer.ToString();
    }

    [Benchmark]
    public async Task<string> SerializeArrayAsync()
    {
        using var writer = new StringWriter();
        await CsvWriter.WriteAsync(_rows, writer, _options).ConfigureAwait(false);
        return writer.ToString();
    }

    [Benchmark]
    public AllocationProbeResult AllocationProbe100k()
    {
        var profiles = new List<SerializationProfile>();
        CsvProfilingHooks.OnSerializationCompleted = profile => profiles.Add(profile);

        try
        {
            using var writer = new StringWriter();
            CsvWriter.Write(_rows, writer, _options);
        }
        finally
        {
            CsvProfilingHooks.OnSerializationCompleted = null;
        }

        var profile = profiles.Count > 0 ? profiles[^1] : default;
        return new AllocationProbeResult(profile.RowsWritten, profile.ColumnCount, profile.AllocatedBytes);
    }

    public readonly record struct AllocationProbeResult(int Rows, int Columns, long AllocatedBytes);

    public sealed record TestRow(int Id, string Name, string Notes);
}
