# Basic Usage

## Who this is for
This guide is for teams adopting CsvForge in standard ASP.NET, worker, or console applications.

## Serialize typed records

```csharp
// samples/CsvForge.Samples.Basic/Program.cs
using CsvForge;

var rows = Enumerable.Range(1, 10)
    .Select(i => new InvoiceRow(i, $"INV-{i:0000}", i * 19.95m));

await using var file = File.Create("invoices.csv");
await CsvSerializer.SerializeAsync(rows, file);

public sealed record InvoiceRow(int Id, string Number, decimal Amount);
```

## Configure writer options

```csharp
// samples/CsvForge.Samples.Excel/Program.cs
using CsvForge;

var options = new CsvOptions
{
    IncludeHeader = true,
    Delimiter = ';'
};

await using var file = File.Create("excel-friendly.csv");
await CsvSerializer.SerializeAsync(rows, file, options);
```

## Enterprise guidance
- Large exports: paginate upstream queries and stream each page immediately.
- Reliability: protect long-running jobs with cancellation tokens and retry policies at job boundaries.
- Observability: track records/second, bytes written, and failures by export type.
- Deployment constraints: test output under container memory limits to ensure no hidden buffering.

## Troubleshooting
### Excel behavior
- Excel may auto-convert IDs to scientific notation; quote or format identifiers before serialization.

### Delimiter mismatch
- If consumers read a single-column CSV, align delimiter with their locale/parser expectation.

### Encoding
- Validate UTF-8 output with representative non-ASCII characters in staging.

## See also
- [Getting Started](./getting-started.md)
- [Advanced Usage](./advanced-usage.md)
- [Compression](./compression.md)
- [FAQ](./faq.md)
