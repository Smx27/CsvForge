# CsvForge Overview

CsvForge is a high-performance CSV engine for .NET 11 designed for teams that need predictable, production-grade export behavior at scale.

## Philosophy

CsvForge is built around four principles:

1. **Performance by default**
   - Optimize throughput and memory profile for large exports.
   - Minimize allocations in hot paths.
2. **Reliability under real workloads**
   - Support checkpointed execution and resumability.
   - Prioritize deterministic output and correctness.
3. **Modern deployment compatibility**
   - Work cleanly with trimming and NativeAOT.
   - Avoid reflection dependence in critical paths.
4. **Practical interoperability**
   - Provide Excel-friendly output behavior.
   - Support streaming compression for operational efficiency.

## What makes CsvForge different

### Hybrid UTF-8/UTF-16 engine

CsvForge can route writes through UTF-8 or UTF-16 optimized flows depending on use case and pipeline composition, enabling strong performance without forcing a one-size-fits-all strategy.

### Roslyn Source Generator

The source generator emits type-specific serializers at compile time, removing reflection overhead and improving AOT compatibility.

### Checkpointed exports

Long-running exports can be segmented into resilient checkpoints, enabling restart/retry behavior in batch and enterprise workflows.

### Streaming compression

CsvForge integrates Gzip and Zip compression in streaming form, reducing memory pressure while keeping output pipeline-friendly.

## Core usage model

1. Define row types.
2. Configure writer options.
3. Select runtime or generated serialization path.
4. Stream output to file/network/storage.
5. Apply checkpoints and compression as needed.

## Recommended reading path

- Start: [`docs/getting-started.md`](getting-started.md)
- Understand internals: [`docs/architecture.md`](architecture.md)
- Optimize generation: [`docs/source-generator.md`](source-generator.md)
- Validate performance: [`docs/performance.md`](performance.md)

## Who should use CsvForge

- API teams exporting operational or analytical datasets
- Platform teams maintaining shared data export infrastructure
- Enterprises requiring resumable, auditable batch export pipelines
- Teams targeting containerized and NativeAOT deployment models
