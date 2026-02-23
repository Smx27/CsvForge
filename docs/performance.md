# Performance

## Who this is for
This guide is for performance-focused teams benchmarking CsvForge under realistic production workloads.

## Measure with included benchmark project

```bash
# benchmarks/CsvForge.Benchmarks
dotnet run -c Release --project benchmarks/CsvForge.Benchmarks/CsvForge.Benchmarks.csproj
```

## Apply high-throughput writer options

```csharp
// samples/CsvForge.Samples.Advanced/Program.cs
using CsvForge;

var options = new CsvWriterOptions
{
    PreferUtf8 = true,
    BufferSize = 128 * 1024
};

await CsvWriter.WriteAsync(rows, outputStream, options);
```

## Enterprise guidance
- Large exports: benchmark with production-like row shape, string lengths, and null ratios.
- Reliability: include soak tests (hours-long runs) to detect leaks and throughput degradation.
- Observability: expose records/sec, bytes/sec, p95 flush latency, and allocation rate.
- Deployment constraints: compare results under actual CPU/memory quotas used in prod.

## Troubleshooting
### Memory pressure
- Lower buffer size when concurrency is high; raise it when per-export throughput is dominant.

### Encoding overhead
- Prefer UTF-8 when downstream systems are UTF-8 native.

### Excel behavior
- If using Excel consumers, measure cost of compatibility settings separately.

## See also
- [Architecture](./architecture.md)
- [Advanced Usage](./advanced-usage.md)
- [Compression](./compression.md)
- [FAQ](./faq.md)
