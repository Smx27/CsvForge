# CsvForge

CsvForge is a high-performance .NET CSV serialization library focused on low allocations, predictable throughput, and ergonomic APIs for both synchronous and asynchronous workflows.

## Installation

Install via NuGet:

```bash
dotnet add package CsvForge
```

Or reference the project directly while developing locally:

```bash
dotnet add ./src/CsvForge/CsvForge.csproj reference
```

## Target frameworks and migration to .NET 11

The project currently multi-targets `net10.0` and `net9.0` as the nearest available framework set. When .NET 11 is available in your SDK matrix, switch the library target framework to `net11.0` (or add it first, then remove older targets once validated).

Recommended migration flow:

1. Update SDK in CI and local dev to a .NET 11 preview/GA build.
2. Change `<TargetFrameworks>` to include `net11.0`.
3. Run full test and benchmark baselines.
4. Remove legacy targets after consumer compatibility review.

## Basic options

`CsvWriterOptions` centralizes common CSV output settings:

- `Delimiter` (default `,`)
- `IncludeHeader` (default `true`)
- `NewLine` (default `\n`)

```csharp
using CsvForge;

var options = new CsvWriterOptions(
    Delimiter: ',',
    IncludeHeader: true,
    NewLine: "\n");
```

## Synchronous serialization example

```csharp
using CsvForge;

var options = new CsvWriterOptions();

// Example API shape (implementation to be added in library):
// var csv = CsvSerializer.Serialize(records, options);
```

## Asynchronous serialization example

```csharp
using CsvForge;

var options = new CsvWriterOptions(Delimiter: ';');

// Example API shape (implementation to be added in library):
// await CsvSerializer.SerializeAsync(records, stream, options, cancellationToken);
```

## Attribute usage

CsvForge is designed to support attribute-based mapping scenarios (column naming, ordering, and ignore rules), for example:

```csharp
public sealed class Trade
{
    // [CsvColumn("trade_id", Order = 0)]
    public required string Id { get; init; }

    // [CsvColumn("qty", Order = 1)]
    public int Quantity { get; init; }

    // [CsvIgnore]
    public string? InternalNotes { get; init; }
}
```

## Delimiter customization

Switch delimiters to support regional formats or tab-separated values:

```csharp
var semicolon = new CsvWriterOptions(Delimiter: ';');
var tabSeparated = new CsvWriterOptions(Delimiter: '\t');
```

## Performance guidance

For best performance:

- Reuse option instances and serializer instances when possible.
- Prefer stream-based async APIs for large datasets.
- Avoid unnecessary intermediate strings in high-volume loops.
- Use benchmarks in `benchmarks/CsvForge.Benchmarks` to validate changes.

Run benchmark suite:

```bash
dotnet run -c Release --project benchmarks/CsvForge.Benchmarks/CsvForge.Benchmarks.csproj
```
