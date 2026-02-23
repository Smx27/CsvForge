# Advanced Usage

## Who this is for
This guide is for teams running high-volume or specialized CSV pipelines that require custom formatting, hybrid engines, and tighter operational controls.

## Use dynamic serialization for shape-flexible rows

```csharp
// src/CsvForge/DynamicCsvSerializer.cs + samples/CsvForge.Samples.Advanced/Program.cs patterns
using CsvForge;

var rows = new List<Dictionary<string, object?>>
{
    new() { ["tenant"] = "acme", ["users"] = 1240, ["active"] = true },
    new() { ["tenant"] = "globex", ["users"] = 875, ["active"] = false }
};

await using var stream = File.Create("tenant-metrics.csv");
await DynamicCsvSerializer.SerializeAsync(rows, stream);
```

## Control writer selection (UTF-8 vs UTF-16)

```csharp
// samples/CsvForge.Samples.Advanced/Program.cs
using CsvForge;

var options = new CsvWriterOptions
{
    PreferUtf8 = true,
    BufferSize = 64 * 1024
};

await using var output = File.Create("hybrid-export.csv");
await CsvWriter.WriteAsync(rows, output, options);
```

## Enterprise guidance
- Large exports: split work into deterministic shards (date range, tenant, partition key).
- Reliability: combine checkpoints with immutable output naming to avoid duplicate partial files.
- Observability: use `CsvProfilingHooks` integration points to capture latency and flush intervals.
- Deployment constraints: pre-size buffers conservatively in serverless or memory-throttled pods.

## Troubleshooting
### Memory pressure
- Reduce row materialization and avoid `ToList()` on very large datasets.

### AOT issues
- Prefer source-generated writers and explicit registration paths when dynamic reflection is restricted.

### Delimiter or quoting edge cases
- Reproduce with a minimal row set and compare behavior using the advanced sample project.

## See also
- [Source Generator](./source-generator.md)
- [Architecture](./architecture.md)
- [Checkpointing](./checkpointing.md)
- [Performance](./performance.md)
