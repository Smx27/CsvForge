---
name: Performance regression
about: Report a measurable throughput/allocation regression
labels: [performance, regression, needs-triage]
---

## Regression summary

Describe what regressed and where.

## Environment

- CsvForge baseline version/commit:
- CsvForge regressed version/commit:
- .NET version:
- OS + hardware:

## Benchmark details

- Benchmark project/command:
- Dataset shape (rows, columns, value characteristics):
- Warmup/iteration settings:

## Results

| Metric | Baseline | Current | Delta |
| --- | --- | --- | --- |
| Throughput |  |  |  |
| Allocations/op |  |  |  |
| Gen0/Gen1/Gen2 |  |  |  |

## Reproduction artifact

Provide code/config so maintainers can reproduce.

```csharp
// Benchmark or repro snippet
```

## Additional context

Include profiler traces, flamegraphs, or runtime diagnostics when available.
