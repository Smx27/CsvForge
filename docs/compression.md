# Compression

## Who this is for
This guide is for teams that need smaller artifacts for network transfer, archival retention, or data-lake ingestion.

## Stream CSV through GZip

```csharp
// samples/CsvForge.Samples.Compression/Program.cs
using System.IO.Compression;
using CsvForge;

await using var file = File.Create("events.csv.gz");
await using var gzip = new GZipStream(file, CompressionLevel.Fastest);
await CsvSerializer.SerializeAsync(rows, gzip);
```

## Tune compression vs throughput

```csharp
// samples/CsvForge.Samples.Compression/Program.cs
await using var gzip = new GZipStream(file, CompressionLevel.Optimal);
await CsvSerializer.SerializeAsync(rows, gzip);
```

## Enterprise guidance
- Large exports: segment by time window and compress each segment independently.
- Reliability: finalize compressed streams (`await using`) to avoid truncated archives.
- Observability: track compression ratio and CPU cost per GB exported.
- Deployment constraints: benchmark `Fastest` vs `Optimal` under your container CPU quotas.

## Troubleshooting
### Corrupt archive errors
- Ensure all wrapping streams are disposed in correct order.

### Memory pressure
- Prefer stream chaining; do not materialize full CSV before compression.

### Consumer compatibility
- Validate downstream tooling supports `.gz` and expected newline conventions.

## See also
- [Basic Usage](./basic-usage.md)
- [Checkpointing](./checkpointing.md)
- [Performance](./performance.md)
- [FAQ](./faq.md)
