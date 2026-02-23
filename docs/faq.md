# FAQ

## Who this is for
This guide is for operators and developers answering common CsvForge adoption and production questions.

## Which sample should I start from?
- Quick start: `samples/CsvForge.Samples.Basic/Program.cs`
- Excel compatibility: `samples/CsvForge.Samples.Excel/Program.cs`
- Compression: `samples/CsvForge.Samples.Compression/Program.cs`
- Checkpointing: `samples/CsvForge.Samples.Checkpointing/Program.cs`
- Source generation: `samples/CsvForge.GeneratedSerializerSample/Program.cs`
- NativeAOT: `samples/CsvForge.Samples.NativeAot/Program.cs`

## How do I handle very large exports?
Stream rows directly to the target stream, page upstream reads, enable checkpointing, and avoid in-memory aggregation.

## How do I make exports reliable?
Use durable checkpoints, deterministic file naming, and idempotent job orchestration so retries do not duplicate data.

## How do I monitor exports?
Track row throughput, bytes written, flush latency, restart count, and export duration. Emit labels for serializer mode (runtime vs generated).

## Why does Excel open my CSV incorrectly?
Check delimiter locale expectations, UTF-8/BOM behavior, and formula-like cell values that Excel auto-converts.

## What about NativeAOT and trimming?
Prefer source-generated contexts and avoid reflection-heavy dynamic code paths where possible.

## Troubleshooting quick checks
- Encoding issues: validate non-ASCII sample rows and verify consumer decoder.
- Delimiter issues: confirm producer and consumer both use the same delimiter.
- Memory pressure: reduce buffering and validate streaming end-to-end.
- AOT issues: run NativeAOT sample before publishing changes.

## See also
- [Getting Started](./getting-started.md)
- [Basic Usage](./basic-usage.md)
- [Checkpointing](./checkpointing.md)
- [Source Generator](./source-generator.md)
