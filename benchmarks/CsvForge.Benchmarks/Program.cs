using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using CsvForge;

BenchmarkRunner.Run<CsvWriterOptionsBenchmarks>();

[MemoryDiagnoser]
public class CsvWriterOptionsBenchmarks
{
    [Benchmark]
    public CsvWriterOptions CreateDefault() => new();
}
