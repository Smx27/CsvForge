# Source Generator

## Who this is for
This guide is for teams that need compile-time generated CSV writers for maximum throughput, trimming, and NativeAOT compatibility.

## Mark models for generation

```csharp
// samples/CsvForge.GeneratedSerializerSample/Program.cs
using CsvForge.Attributes;

[CsvSerializable]
public sealed partial record OrderRow(
    int Id,
    string Customer,
    decimal Total,
    DateTimeOffset CreatedAt);
```

## Use generated serializers

```csharp
// samples/CsvForge.GeneratedSerializerSample/Program.cs
using CsvForge;

await using var stream = File.Create("orders-generated.csv");
await CsvSerializer.SerializeAsync(orders, stream, CsvSerializationContext.Default.OrderRow);
```

## Enterprise guidance
- Large exports: generated writers reduce per-row reflection and stabilize tail latency.
- Reliability: lock package/source-generator versions across services to avoid schema drift.
- Observability: include generator version in export metadata for incident triage.
- Deployment constraints: generated context registration is preferred in trimmed and AOT builds.

## Troubleshooting
### Missing generated types
- Confirm the model is `partial` where required and the project references `CsvForge.SourceGenerator`.

### AOT or trimming failures
- Use explicit generated context APIs; avoid runtime-only reflection paths.

### Build performance
- Scope generator inputs to relevant assemblies when working in large mono-repos.

## See also
- [Advanced Usage](./advanced-usage.md)
- [Architecture](./architecture.md)
- [Developer Guide](./developer-guide.md)
- [FAQ](./faq.md)
