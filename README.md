# CsvForge

CsvForge is a high-throughput CSV export library for modern .NET workloads that need to stream large datasets with predictable memory usage, resumability, and deployment flexibility.

## Hero summary

CsvForge combines a **hybrid UTF-8/UTF-16 write engine**, **compile-time source generation**, **checkpoint-aware export flows**, **built-in compression**, and **NativeAOT-friendly patterns** so teams can ship production CSV pipelines without relying on reflection-heavy hot paths.

### Key features

- **Hybrid UTF-8/UTF-16 pipeline** that selects the best writer path for your scenario.
- **Source generator support** (`[CsvSerializable]`) for compile-time writers and trim-safe serialization contexts.
- **Checkpointing support** for resumable exports and durable progress boundaries.
- **Compression options** (GZip and ZIP) for storage/network efficiency.
- **NativeAOT + trimming guidance** via generated contexts and strict mode.
- Streaming APIs for sync/async export flows with low allocation overhead.

---

## Quick start in <60 seconds

### 1) Install package

```bash
dotnet add package CsvForge
```

### 2) Create a minimal model

```csharp
public sealed class UserRow
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
```

### 3) Write with one `CsvWriter` call

```csharp
using CsvForge;

var rows = new[]
{
    new UserRow { Id = 1, Name = "Ada" },
    new UserRow { Id = 2, Name = "Linus" }
};

CsvWriter.Write(rows, "users.csv");
```

### 4) Expected output (`users.csv`)

```csv
Id,Name
1,Ada
2,Linus
```

---

## Feature comparison

| Capability | CsvForge | Typical traditional CSV library |
| --- | --- | --- |
| Metadata strategy | Cached and/or source-generated writers | Reflection-heavy per-type/per-call paths |
| UTF handling | Hybrid UTF-8/UTF-16 engine | Usually single-path text writer flow |
| Checkpointing/resume | First-class checkpointing guidance and samples | Commonly not built in |
| Compression | Built-in GZip and ZIP options | Often external/manual wrapping |
| NativeAOT/trimming posture | Generated contexts + strict mode guidance | Limited AOT support; reflection can break under trimming |
| Large export orientation | Streaming-first APIs and benchmark guidance | Often optimized for simpler small/medium writes |

---

## Architecture summary

CsvForge uses an engine selector that routes writes through UTF-8 or UTF-16 paths, can use generated type writers instead of runtime metadata, and supports checkpoint coordination for resumable long-running exports.

- Architecture deep dive: [`docs/architecture.md`](docs/architecture.md)
- Source generator deep dive: [`docs/source-generator.md`](docs/source-generator.md)

---

## Samples index

| Project | Feature focus | Run command |
| --- | --- | --- |
| `CsvForge.Samples.Basic` | Baseline CSV writing patterns | `dotnet run --project samples/CsvForge.Samples.Basic/CsvForge.Samples.Basic.csproj` |
| `CsvForge.Samples.Advanced` | Advanced writer options and tuning | `dotnet run --project samples/CsvForge.Samples.Advanced/CsvForge.Samples.Advanced.csproj` |
| `CsvForge.Samples.Excel` | Excel compatibility behavior | `dotnet run --project samples/CsvForge.Samples.Excel/CsvForge.Samples.Excel.csproj` |
| `CsvForge.Samples.Checkpointing` | Resumable export and checkpoint flow | `dotnet run --project samples/CsvForge.Samples.Checkpointing/CsvForge.Samples.Checkpointing.csproj` |
| `CsvForge.Samples.Compression` | GZip and ZIP export paths | `dotnet run --project samples/CsvForge.Samples.Compression/CsvForge.Samples.Compression.csproj` |
| `CsvForge.GeneratedSerializerSample` | Source generator-based serialization | `dotnet run --project samples/CsvForge.GeneratedSerializerSample/CsvForge.GeneratedSerializerSample.csproj` |
| `CsvForge.Samples.NativeAot` | NativeAOT-friendly usage pattern | `dotnet run --project samples/CsvForge.Samples.NativeAot/CsvForge.Samples.NativeAot.csproj` |

See also: [`samples/README.md`](samples/README.md)

---

## Performance highlights

CsvForge is designed for high throughput and low allocation pressure in export-heavy workloads:

- Metadata caching and generated writers reduce runtime introspection overhead.
- Streaming APIs avoid loading full exports into memory.
- Buffering and pooled internals reduce write amplification and GC churn.
- Compression paths are integrated for end-to-end export workflows.

### Caveats

Performance depends on row shape, null ratios, string length distributions, storage throughput, compression mode, and runtime environment (CPU quotas, GC mode, container limits). Always benchmark with production-like data.

- Performance guide: [`docs/performance.md`](docs/performance.md)
- Benchmark methodology/project: `benchmarks/CsvForge.Benchmarks`

Run benchmarks:

```bash
dotnet run -c Release --project benchmarks/CsvForge.Benchmarks/CsvForge.Benchmarks.csproj
```

---

## Troubleshooting and FAQ

- FAQ: [`docs/faq.md`](docs/faq.md)
- Troubleshooting topics:
  - Checkpointing: [`docs/checkpointing.md`](docs/checkpointing.md)
  - Compression: [`docs/compression.md`](docs/compression.md)
  - Advanced usage: [`docs/advanced-usage.md`](docs/advanced-usage.md)
  - Performance diagnostics: [`docs/performance.md`](docs/performance.md)

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

## Community and contribution

- Contributing guide: [`CONTRIBUTING.md`](CONTRIBUTING.md)
- Code of conduct: [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md)

---

## License

Specify your project license here (for example, `MIT`).
