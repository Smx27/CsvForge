# Developer Guide

## Who this is for
This guide is for contributors extending CsvForge internals, tests, and source generation behavior.

## Local development loop

```bash
# restore and build
dotnet build CsvForge.sln

# run tests
dotnet test tests/CsvForge.Tests/CsvForge.Tests.csproj
```

## Add a new writer behavior test

```csharp
// tests/CsvForge.Tests/CsvWriterOptionsTests.cs pattern
[Fact]
public async Task UsesConfiguredDelimiter()
{
    var options = new CsvOptions { Delimiter = ';' };
    // Arrange rows, serialize, assert delimiter behavior.
}
```

## Sample-driven manual validation
- Basic path: `samples/CsvForge.Samples.Basic/Program.cs`
- Advanced path: `samples/CsvForge.Samples.Advanced/Program.cs`
- Generator path: `samples/CsvForge.GeneratedSerializerSample/Program.cs`
- Checkpoint path: `samples/CsvForge.Samples.Checkpointing/Program.cs`

## Enterprise guidance
- Large exports: validate changes with representative high-volume data in benchmarks.
- Reliability: include regression tests for restart/retry and partial write scenarios.
- Observability: preserve low-overhead hooks when adding instrumentation.
- Deployment constraints: test trimmed and NativeAOT sample behavior before release.

## Troubleshooting
### Failing golden files
- Regenerate only when intentional generator changes occur, then review diffs line-by-line.

### AOT regressions
- Verify `samples/CsvForge.Samples.NativeAot/Program.cs` scenarios after serialization API changes.

### Memory regressions
- Use benchmark comparisons and allocation-focused profiling in Release builds.

## See also
- [Architecture](./architecture.md)
- [Source Generator](./source-generator.md)
- [Performance](./performance.md)
- [FAQ](./faq.md)
