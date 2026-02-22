# CsvForge

`CsvForge` is a production-focused, high-performance CSV writer for modern .NET applications.
It is designed for **very large exports** (100,000+ rows), with a strong emphasis on:

- low allocations,
- fast streaming I/O,
- predictable behavior,
- clean APIs for sync and async workflows.

---

## Why CsvForge?

CSV export often becomes a bottleneck when applications need to emit large datasets quickly.
`CsvForge` focuses on hot-path performance by combining metadata caching, streaming, pooled buffers, and minimal per-row overhead.

### Core capabilities

- Write CSV from `IEnumerable<T>`, `List<T>`, and arrays.
- Automatic header generation from model properties.
- Custom delimiters (`,`, `;`, `|`, `\t`, or any custom `char`).
- Attribute-driven column naming and ordering.
- `JsonPropertyName` support from `System.Text.Json`.
- Dynamic object and dictionary-like row support.
- File path, `Stream`, and `TextWriter` targets.
- Optional GZip or ZIP-compressed output via `CsvOptions.Compression`.
- Synchronous and asynchronous APIs.
- Standards-compliant escaping for delimiters, quotes, and line breaks.
- Internal metadata cache to avoid repeated reflection.
- Allocation-aware implementation using pooling and optimized formatting paths.

---

## Target framework

- **Primary target:** `.NET 11`

> If your environment uses preview SDKs, ensure your CI and local setup include the matching .NET 11 SDK/runtime.

---

## Installation

### Package Manager

```powershell
Install-Package CsvForge
```

### .NET CLI

```bash
dotnet add package CsvForge
```

### PackageReference

```xml
<ItemGroup>
  <PackageReference Include="CsvForge" Version="1.0.0" />
</ItemGroup>
```

---

## Quick start

### 1) Basic export to file

```csharp
using CsvForge;

var rows = new List<User>
{
    new() { Id = 1, Name = "Ada", Email = "ada@example.com" },
    new() { Id = 2, Name = "Linus", Email = "linus@example.com" }
};

await CsvWriter.WriteToFileAsync(rows, "users.csv");
```

### 2) Write to stream

```csharp
using CsvForge;

await using var file = File.Create("orders.csv");
CsvWriter.Write(orders, file);
```

### 3) Write to `TextWriter`

```csharp
using CsvForge;

using var sw = new StringWriter();
CsvWriter.Write(products, sw);
var csv = sw.ToString();
```

---

## Advanced usage

### Configure options

```csharp
using CsvForge;
using System.Globalization;
using System.Text;

var options = new CsvOptions
{
    Delimiter = ';',
    IncludeHeader = true,
    Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
    BufferSize = 64 * 1024,
    FormatProvider = CultureInfo.InvariantCulture
};

await CsvWriter.WriteToFileAsync(data, "export.csv", options);
```

Defaults today:

- `Encoding`: UTF-8 without BOM (`new UTF8Encoding(false)`)
- `NewLineBehavior`: `CsvNewLineBehavior.Environment` (uses platform newline)

Use `NewLineBehavior = CsvNewLineBehavior.Lf` or `CrLf` for deterministic cross-platform files.

### Excel compatibility mode

For Excel-first exports, enable `ExcelCompatibility`:

```csharp
var options = new CsvOptions
{
    ExcelCompatibility = true
};

CsvWriter.Write(data, stream, options);
```

Semantics:
- Defaults row terminators to `\r\n` when no explicit newline is provided.
- Uses `;` delimiter fallback for cultures with decimal comma (for example `fr-FR`) when delimiter remains default `,`.
- Emits UTF-8 BOM for stream/file targets.
- Normalizes embedded newlines inside escaped fields to CRLF while preserving quote escaping (`""`).

You can override row terminators explicitly using `ExplicitNewLine` (for example, `"\r\n"`).

### Compressed output (GZip / ZIP)

```csharp
var gzipOptions = new CsvOptions
{
    Compression = CsvCompressionMode.Gzip
};

await CsvWriter.WriteToFileAsync(data, "export.csv.gz", gzipOptions);

var zipOptions = new CsvOptions
{
    Compression = CsvCompressionMode.Zip
};

await CsvWriter.WriteToFileAsync(data, "export.zip", zipOptions);
```

When `Compression = Zip`, CsvForge writes directly into a single `data.csv` entry without buffering the full CSV payload in memory.

---

### Tab-delimited output

```csharp
var options = new CsvOptions { Delimiter = '\t' };
CsvWriter.Write(data, "report.tsv", options);
```

### Pipe-delimited output, no header

```csharp
var options = new CsvOptions
{
    Delimiter = '|',
    IncludeHeader = false
};

CsvWriter.Write(data, stream, options);
```

---

## Column mapping and ordering

`CsvForge` supports custom column names and order through `CsvColumnAttribute`.

```csharp
using CsvForge;
using System.Text.Json.Serialization;

public sealed class InvoiceRow
{
    [CsvColumn("invoice_id", Order = 0)]
    public int Id { get; init; }

    [JsonPropertyName("customer_name")]
    [CsvColumn(Order = 1)]
    public string Customer { get; init; } = string.Empty;

    [CsvColumn("amount", Order = 2)]
    public decimal Total { get; init; }
}
```

Naming precedence:

1. `CsvColumnAttribute.Name`
2. `JsonPropertyNameAttribute.Name`
3. Property name

Ordering precedence:

1. `CsvColumnAttribute.Order`
2. Default property order

---

## Dynamic data support

You can export collections of dynamic or dictionary-like rows.

```csharp
var rows = new List<Dictionary<string, object?>>
{
    new() { ["id"] = 1, ["name"] = "alpha", ["active"] = true },
    new() { ["id"] = 2, ["name"] = "beta", ["active"] = false }
};

await CsvWriter.WriteToFileAsync(rows, "dynamic.csv");
```

For heterogeneous dynamic shapes, define a stable schema policy in your application (recommended: union headers + missing fields as empty values).

---

## Async streaming for large exports

For very large datasets, prefer asynchronous streaming to minimize memory pressure and keep request threads unblocked.

```csharp
await foreach (var row in repository.StreamRowsAsync(ct))
{
    // produce rows lazily
}

await CsvWriter.WriteToFileAsync(repository.StreamRowsAsync(ct), "huge-export.csv", cancellationToken: ct);
```

Recommended for:

- background jobs,
- data pipelines,
- web API endpoints generating downloadable files.

---

## CSV escaping behavior

`CsvForge` follows standard CSV escaping rules:

- Fields containing delimiter, quote (`"`), `\r`, or `\n` are quoted.
- Inner quotes are escaped by doubling: `"` -> `""`.
- `null` values are written as empty fields.

Example:

Input value:

```text
He said, "hello"
```

Output field:

```text
"He said, ""hello"""
```

---

## Performance profile

`CsvForge` is designed for high throughput and low GC pressure:

- **Metadata cache** avoids repeated reflection for each row.
- **Compiled accessors/delegates** eliminate per-property reflection in hot paths.
- **Buffered writing** reduces I/O calls.
- **Pooling** (`ArrayPool`, builders) lowers allocation frequency.
- **Streaming-first APIs** avoid loading full exports into memory.

### Benchmark strategy

The benchmark suite compares:

- `CsvForge` optimized sync writer,
- `CsvForge` optimized async writer,
- naive reflection/string-concatenation baseline.

Scenarios include small, medium, and large row counts (e.g., 1k, 10k, 100k+), plus GZip and ZIP compression paths.

---

## Developer guide

### Repository layout

```text
/src/CsvForge                # Library source
/tests/CsvForge.Tests        # Unit tests
/benchmarks/CsvForge.Benchmarks # BenchmarkDotNet scenarios
README.md
```

### Local development workflow

1. Restore dependencies.
2. Build in Release mode.
3. Run unit tests.
4. Run benchmarks (optional but recommended before release).

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
dotnet run -c Release --project benchmarks/CsvForge.Benchmarks/CsvForge.Benchmarks.csproj
```

### Performance engineering guidelines

When contributing performance-sensitive changes:

- Avoid per-row reflection and dynamic dispatch.
- Prefer spans, pooled buffers, and reusable builders.
- Keep synchronous and asynchronous code paths both optimized.
- Validate with allocation and throughput benchmarks before merging.
- Add edge-case tests for quoting, delimiters, and null handling.

### API design guidelines

- Keep public APIs simple and discoverable.
- Introduce advanced behavior through `CsvOptions` rather than overload explosion.
- Preserve backward compatibility whenever possible.
- Document non-obvious behavior (e.g., dynamic schema handling).

---

## Troubleshooting

### Unexpected delimiter behavior

Verify `CsvOptions.Delimiter` is set correctly and does not conflict with source data assumptions.

### Numbers/dates formatted differently than expected

Set `CsvOptions.FormatProvider` explicitly (e.g., `CultureInfo.InvariantCulture`) for consistent formatting across environments.

### Large exports are slow

- Increase `CsvOptions.BufferSize`.
- Prefer `WriteAsync` with streaming sources.
- Ensure release build and server GC for production workloads.

---

## Versioning and compatibility

- Semantic Versioning is used for package releases.
- Breaking API changes are introduced only in major versions.

---

## License

Specify your project license here (for example, `MIT`).

