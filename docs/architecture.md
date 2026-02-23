# Architecture

## Who this is for
This guide is for architects and senior engineers who need to understand CsvForge internals for scale, reliability, and maintainability decisions.

## System overview
CsvForge uses a hybrid writer architecture that selects UTF-8 or UTF-16 paths, supports optional source-generated serializers, and can coordinate checkpointing for resumable exports.

## Hybrid UTF-8 / UTF-16 engine

```text
┌──────────────────────────┐
│      CsvSerializer       │
└─────────────┬────────────┘
              │
      ┌───────▼────────┐
      │ CsvEngineSelector│
      └───────┬────────┘
      PreferUtf8? │
        ┌─────────┴───────────┐
        │                     │
┌───────▼────────┐   ┌────────▼────────┐
│   Utf8CsvWriter │   │   Utf16CsvWriter│
│ CsvUtf8Buffer   │   │ CsvCharBuffer   │
└───────┬────────┘   └────────┬────────┘
        └──────────┬──────────┘
                   ▼
             Output Stream
```

## Source generation flow

```text
[Model + [CsvSerializable]]
            │
            ▼
 CsvForge.SourceGenerator
            │ emits
            ▼
 Generated type writers + CsvSerializationContext
            │
            ▼
 CsvSerializer.SerializeAsync(..., context)
```

## Checkpoint pipeline

```text
Rows -> Writer -> Flush Boundary -> Checkpoint Coordinator -> Durable Checkpoint Store
  ^                                                            |
  |--------------------------- Resume Offset -------------------|
```

## Enterprise guidance
- Large exports: favor UTF-8 path for throughput when consumers accept UTF-8.
- Reliability: checkpoint at durable flush boundaries to avoid replay ambiguity.
- Observability: emit metrics around engine selection, flush size, and checkpoint cadence.
- Deployment constraints: use generated contexts in trimmed/AOT targets to avoid reflection pitfalls.

## Troubleshooting
### Engine mismatch
- If output behavior differs between environments, log effective options (`PreferUtf8`, delimiter, newline).

### Memory pressure
- Tune buffer sizes (`CsvUtf8Buffer`/`CsvCharBuffer`) based on row width distribution.

### AOT issues
- Validate generated writer registration in publish pipeline before production rollout.

## See also
- [Source Generator](./source-generator.md)
- [Checkpointing](./checkpointing.md)
- [Performance](./performance.md)
- [Developer Guide](./developer-guide.md)
