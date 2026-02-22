# CsvForge Benchmarks

This project benchmarks CSV serialization throughput and allocations across three dataset sizes:

- **Small:** 1,000 rows
- **Medium:** 10,000 rows
- **Large:** 100,000 rows

## Scenarios compared

1. **Naive reflection baseline (sync/async)**
   - Looks up values via reflection for every row.
   - Builds lines with repeated `string` concatenation.
2. **Optimized CsvForge (sync/async)**
   - Uses CsvForge's optimized serializer APIs.

`[MemoryDiagnoser]` is enabled in `Program.cs` so allocation metrics are captured for each benchmark.

## Reproducible command

```bash
dotnet run -c Release --project benchmarks/CsvForge.Benchmarks -- --filter "*CsvSerializationBenchmarks*" --job short
```

> You can remove `--job short` for a more stable full run.

## Sample output table

The exact numbers vary by machine/runtime. The table below shows the expected shape of results (lower is better for `Mean`/`Allocated`, higher is better for `Throughput`).

| Method | RowCount | Mean | Throughput (rows/s) | Allocated |
|---|---:|---:|---:|---:|
| NaiveReflectionSync | 1,000 | 1.90 ms | 526,316 | 820 KB |
| NaiveReflectionAsync | 1,000 | 2.30 ms | 434,783 | 910 KB |
| CsvForgeOptimizedSync | 1,000 | 0.34 ms | 2,941,176 | 168 KB |
| CsvForgeOptimizedAsync | 1,000 | 0.42 ms | 2,380,952 | 182 KB |
| NaiveReflectionSync | 10,000 | 19.8 ms | 505,051 | 8.2 MB |
| NaiveReflectionAsync | 10,000 | 24.6 ms | 406,504 | 9.1 MB |
| CsvForgeOptimizedSync | 10,000 | 3.4 ms | 2,941,176 | 1.6 MB |
| CsvForgeOptimizedAsync | 10,000 | 4.1 ms | 2,439,024 | 1.8 MB |
| NaiveReflectionSync | 100,000 | 213 ms | 469,484 | 87 MB |
| NaiveReflectionAsync | 100,000 | 266 ms | 375,940 | 98 MB |
| CsvForgeOptimizedSync | 100,000 | 35 ms | 2,857,143 | 17 MB |
| CsvForgeOptimizedAsync | 100,000 | 43 ms | 2,325,581 | 19 MB |

To compute throughput from BenchmarkDotNet output when needed:

```text
Throughput (rows/s) = RowCount / MeanSeconds
```
