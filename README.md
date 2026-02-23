# CsvForge

> **CsvForge** is a flagship .NET 11 CSV engine for enterprise-grade data exports—built for speed, reliability, and NativeAOT from day one.

[![NuGet](https://img.shields.io/nuget/v/CsvForge.svg)](#)
[![Downloads](https://img.shields.io/nuget/dt/CsvForge.svg)](#)
[![Build](https://img.shields.io/github/actions/workflow/status/your-org/CsvForge/ci.yml?branch=main)](#)
[![License](https://img.shields.io/github/license/your-org/CsvForge)](#)
[![Docs](https://img.shields.io/badge/docs-online-brightgreen)](#)

CsvForge combines a **hybrid UTF-8/UTF-16 writer**, **Roslyn Source Generator**, **checkpointed export orchestration**, and **streaming compression** to deliver predictable, high-throughput CSV pipelines for APIs, services, batch jobs, and data platforms.

---

## Why CsvForge

- ⚡ **Performance-first engine** with low-allocation hot paths.
- 🧠 **Roslyn-generated serializers** for zero-reflection runtime execution.
- 📦 **NativeAOT + trimming ready** for modern cloud-native deployment.
- 🔁 **Checkpointed batch exports** for resumable long-running jobs.
- 🗜️ **Streaming Gzip/Zip compression** for large data transfer workflows.
- 📊 **Excel compatibility mode** for real-world spreadsheet consumers.

---

## Quick Start

### Install

```bash
dotnet add package CsvForge
```

### Minimal example

```csharp
using CsvForge;

public sealed class CustomerRow
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Tier { get; init; } = string.Empty;
}

var rows = new[]
{
    new CustomerRow { Id = 1, Name = "Acme", Tier = "Enterprise" },
    new CustomerRow { Id = 2, Name = "Northwind", Tier = "SMB" }
};

CsvWriter.Write(rows, "customers.csv", new CsvWriterOptions
{
    IncludeHeader = true,
    ExcelCompatibilityMode = true
});
```

Output:

```csv
Id,Name,Tier
1,Acme,Enterprise
2,Northwind,SMB
```

---

## Performance Benchmarks

CsvForge is designed for production throughput under realistic memory constraints.

### Benchmark focus areas

- UTF-8 and UTF-16 writer throughput
- Allocation profile (Gen0/Gen1 pressure)
- Generated writer vs runtime metadata path
- Compression overhead under streaming workloads
- Checkpoint frequency and recovery impact

Run local benchmarks:

```bash
dotnet run -c Release --project benchmarks/CsvForge.Benchmarks/CsvForge.Benchmarks.csproj
```

For methodology and reproducibility guidelines, see [`docs/performance.md`](docs/performance.md).

---

## CsvForge vs Traditional CSV Libraries

| Capability | CsvForge | Traditional CSV Libraries |
| --- | --- | --- |
| Serialization strategy | Runtime cache + Roslyn source generation | Reflection-heavy runtime mapping |
| Hot path allocations | Span-centric, allocation-aware internals | Often string/object-centric |
| NativeAOT compatibility | First-class design target | Often limited due to reflection |
| Checkpoint/resume | Built-in export checkpoint model | Usually app-level custom implementation |
| Compression | Integrated streaming Gzip/Zip support | Usually external wrappers |
| Excel compatibility | Dedicated mode with practical defaults | Inconsistent/manual options |

---

## Architecture Overview

CsvForge uses a layered architecture optimized for deterministic behavior and deployment flexibility:

1. **Core Writer Engine**
   - Hybrid UTF-8/UTF-16 selection pipeline
   - Buffer and formatter components tuned for high-throughput output
2. **Type Serialization Layer**
   - Roslyn Source Generator emits type-specific writers
   - Zero-reflection runtime path when generated writers are available
3. **Execution Orchestration Layer**
   - Checkpoint coordinator for resumable exports
   - Compression streams and compatibility options as composable stages

Start here:

- [`docs/overview.md`](docs/overview.md)
- [`docs/architecture.md`](docs/architecture.md)
- [`docs/source-generator.md`](docs/source-generator.md)

---

## Samples

- [`samples/CsvForge.Samples.Basic`](samples/CsvForge.Samples.Basic)
- [`samples/CsvForge.Samples.Advanced`](samples/CsvForge.Samples.Advanced)
- [`samples/CsvForge.Samples.Checkpointing`](samples/CsvForge.Samples.Checkpointing)
- [`samples/CsvForge.Samples.Compression`](samples/CsvForge.Samples.Compression)
- [`samples/CsvForge.Samples.Excel`](samples/CsvForge.Samples.Excel)
- [`samples/CsvForge.Samples.NativeAot`](samples/CsvForge.Samples.NativeAot)
- [`samples/CsvForge.GeneratedSerializerSample`](samples/CsvForge.GeneratedSerializerSample)

Sample guide: [`samples/README.md`](samples/README.md)

---

## Documentation

- Getting started: [`docs/getting-started.md`](docs/getting-started.md)
- Basic usage: [`docs/basic-usage.md`](docs/basic-usage.md)
- Advanced usage: [`docs/advanced-usage.md`](docs/advanced-usage.md)
- Checkpointing: [`docs/checkpointing.md`](docs/checkpointing.md)
- Compression: [`docs/compression.md`](docs/compression.md)
- FAQ: [`docs/faq.md`](docs/faq.md)

---

## Roadmap

- [ ] Full async IAsyncEnumerable pipeline optimization
- [ ] Additional delimiter/quoting profiles for region-specific formats
- [ ] Rich schema evolution support for versioned exports
- [ ] Wider benchmark matrix (.NET runtimes, ARM64/x64, Linux/Windows)
- [ ] Production diagnostics package (events/metrics dashboards)

See open work in [GitHub Issues](https://github.com/your-org/CsvForge/issues).

---

## Contributing

Contributions are welcome—from bug fixes to benchmark improvements and source generator enhancements.

- Read [`CONTRIBUTING.md`](CONTRIBUTING.md)
- Follow [`CODE_OF_CONDUCT.md`](CODE_OF_CONDUCT.md)
- Start with issues tagged `good first issue` and `help wanted`

---

## License

CsvForge is released under the MIT License. See [`LICENSE`](LICENSE) for details.
