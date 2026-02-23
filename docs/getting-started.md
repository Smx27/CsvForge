# Getting Started with CsvForge

## Who this is for
This guide is for .NET developers who want a fast path to produce CSV output in production services, background jobs, or command-line tooling.

## Install and create your first export

```bash
dotnet add package CsvForge
```

```csharp
// samples/CsvForge.Samples.Basic/Program.cs
using CsvForge;

var records = new[]
{
    new Customer(1, "Ada", "ada@example.com"),
    new Customer(2, "Linus", "linus@example.com")
};

await using var stream = File.Create("customers.csv");
await CsvSerializer.SerializeAsync(records, stream);

public sealed record Customer(int Id, string Name, string Email);
```

## Pick a sample project to start from
- Basic flow: `samples/CsvForge.Samples.Basic/Program.cs`
- Excel-safe output: `samples/CsvForge.Samples.Excel/Program.cs`
- Compression pipeline: `samples/CsvForge.Samples.Compression/Program.cs`
- Checkpoint/restart: `samples/CsvForge.Samples.Checkpointing/Program.cs`
- Source-generation path: `samples/CsvForge.GeneratedSerializerSample/Program.cs`
- AOT constraints: `samples/CsvForge.Samples.NativeAot/Program.cs`

## Enterprise guidance
- For large exports, stream directly to files or network streams instead of buffering full CSV in memory.
- For reliability, pair exports with idempotent job IDs and checkpoint state so retries resume cleanly.
- For observability, emit row counts, elapsed time, and flush cadence metrics.
- For deployment constraints (containers/serverless), keep temporary disk usage bounded and prefer chunked writes.

## Troubleshooting
### Encoding and BOM
- If Excel opens UTF-8 text incorrectly, use the Excel-oriented sample (`samples/CsvForge.Samples.Excel/Program.cs`) and verify encoding options.

### Delimiters and locale
- In regions where Excel expects semicolons, configure delimiter behavior as shown in the Excel sample.

### Memory pressure
- If exports trigger high GC, reduce in-memory transformations and stream row-by-row.

## See also
- [Basic Usage](./basic-usage.md)
- [Architecture](./architecture.md)
- [Performance](./performance.md)
- [FAQ](./faq.md)
