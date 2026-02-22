# CsvForge Benchmarks

This benchmark project compares CsvForge serialization engines using the same dataset across:

- **Reflection fallback UTF-16 writer** (`EnableRuntimeMetadataFallback = true`)
- **Generated UTF-16 writer** (`TextWriter` target)
- **Generated UTF-8 writer to `Stream`**
- **Generated UTF-8 writer to `IBufferWriter<byte>`**

The engine matrix is implemented in `GeneratorEngineMatrixBenchmarks`.

`CsvSerializationBenchmarks` also includes compression-path scenarios (`CsvCompressionMode.Gzip` and `CsvCompressionMode.Zip`) for both sync and async stream output.

## Scale and scenario matrix

`GeneratorEngineMatrixBenchmarks` runs the following row scales:

- **100,000 rows**
- **1,000,000 rows**

For each scale, it also runs CSV dialect/escaping scenarios to isolate overhead:

1. `CommaLfNoEscaping` (`Delimiter=,`, `NewLine=LF`, mostly non-escaped payload)
2. `SemicolonCrLfNoEscaping` (`Delimiter=;`, `NewLine=CRLF`, mostly non-escaped payload)
3. `CommaCrLfEscaping` (`Delimiter=,`, `NewLine=CRLF`, RFC4180 stress payload with commas, quotes, and embedded newlines)

## Reproducible command

```bash
dotnet run -c Release --framework net10.0 --project benchmarks/CsvForge.Benchmarks -- --filter "*GeneratorEngineMatrixBenchmarks*" --job short
```

> Remove `--job short` for more stable, publication-grade numbers.

## Normalized metrics

Use these formulas with BenchmarkDotNet output:

- `rows/sec = RowCount / MeanSeconds`
- `normalized rows/sec = method rows/sec / ReflectionFallback_Utf16 rows/sec`
- `memory ratio = method AllocatedBytes / ReflectionFallback_Utf16 AllocatedBytes`

### Reporting template (fill with your local run)

| Scenario | RowCount | Method | Mean | Rows/sec | Normalized Rows/sec (↑ better) | Allocated | Memory Ratio (↓ better) |
|---|---:|---|---:|---:|---:|---:|---:|
| CommaLfNoEscaping | 100,000 | ReflectionFallback_Utf16 (baseline) | ... | ... | 1.00x | ... | 1.00x |
| CommaLfNoEscaping | 100,000 | SourceGenerated_Utf16 | ... | ... | ...x | ... | ...x |
| CommaLfNoEscaping | 100,000 | SourceGenerated_Utf8_Stream | ... | ... | ...x | ... | ...x |
| CommaLfNoEscaping | 100,000 | SourceGenerated_Utf8_IBufferWriter | ... | ... | ...x | ... | ...x |
| SemicolonCrLfNoEscaping | 1,000,000 | ReflectionFallback_Utf16 (baseline) | ... | ... | 1.00x | ... | 1.00x |
| SemicolonCrLfNoEscaping | 1,000,000 | SourceGenerated_Utf16 | ... | ... | ...x | ... | ...x |
| SemicolonCrLfNoEscaping | 1,000,000 | SourceGenerated_Utf8_Stream | ... | ... | ...x | ... | ...x |
| SemicolonCrLfNoEscaping | 1,000,000 | SourceGenerated_Utf8_IBufferWriter | ... | ... | ...x | ... | ...x |
| CommaCrLfEscaping | 1,000,000 | ReflectionFallback_Utf16 (baseline) | ... | ... | 1.00x | ... | 1.00x |
| CommaCrLfEscaping | 1,000,000 | SourceGenerated_Utf16 | ... | ... | ...x | ... | ...x |
| CommaCrLfEscaping | 1,000,000 | SourceGenerated_Utf8_Stream | ... | ... | ...x | ... | ...x |
| CommaCrLfEscaping | 1,000,000 | SourceGenerated_Utf8_IBufferWriter | ... | ... | ...x | ... | ...x |

## Reading results

- Compare **normalized rows/sec** to see pure throughput deltas versus reflection fallback.
- Compare **memory ratio** to quantify allocation reductions.
- Compare `CommaLfNoEscaping` vs `SemicolonCrLfNoEscaping` to isolate delimiter/newline overhead.
- Compare `*NoEscaping` vs `CommaCrLfEscaping` to quantify RFC4180 escaping overhead.
