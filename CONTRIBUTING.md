# Contributing to CsvForge

Thank you for helping improve CsvForge. This project is performance-critical infrastructure software, and we treat contributions with production rigor.

## Table of Contents

- [Development Setup](#development-setup)
- [Coding Standards](#coding-standards)
- [Source Generator Guidelines](#source-generator-guidelines)
- [Benchmark Validation Rules](#benchmark-validation-rules)
- [Testing Expectations](#testing-expectations)
- [Pull Request Process](#pull-request-process)

---

## Development Setup

### Prerequisites

- .NET 11 SDK (preview channel as required)
- Git
- Optional: BenchmarkDotNet-friendly environment for stable benchmark runs

### Clone and build

```bash
git clone https://github.com/your-org/CsvForge.git
cd CsvForge
dotnet restore
dotnet build CsvForge.sln -c Release
```

### Run tests

```bash
dotnet test tests/CsvForge.Tests/CsvForge.Tests.csproj -c Release
```

### Run benchmarks

```bash
dotnet run -c Release --project benchmarks/CsvForge.Benchmarks/CsvForge.Benchmarks.csproj
```

---

## Coding Standards

CsvForge follows **performance-first engineering** principles.

### Core rules

1. **Prefer `Span<T>`, `ReadOnlySpan<T>`, and pooled buffers** in hot paths.
2. **Avoid allocations in per-row/per-cell loops** unless justified and documented.
3. **Do not introduce reflection-based work in runtime critical paths**.
4. **Keep branches predictable** and avoid hidden culture-sensitive conversions.
5. **Guard correctness first**: CSV escaping, encoding safety, and deterministic output are non-negotiable.

### API and style

- Keep public APIs explicit and stable.
- Follow existing naming conventions and project layout.
- Add XML docs for new public types/members.
- Prefer small, composable internal components over monolithic implementations.

---

## Source Generator Guidelines

The Roslyn generator is a strategic component and must remain robust under incremental builds and AOT scenarios.

- Place generator logic under `src/CsvForge.SourceGenerator`.
- Keep diagnostics actionable, precise, and user-friendly.
- Preserve deterministic output ordering for generated members/files.
- Add/maintain golden-file tests for generated output when behavior changes.
- Ensure generated code avoids reflection and is trimming-safe.

When changing generator behavior:

1. Update generator tests in `tests/CsvForge.Tests`.
2. Update approval/golden files as needed.
3. Document user-visible behavior changes in docs and PR notes.

---

## Benchmark Validation Rules

Any change touching writer internals, formatting, encoding, metadata caches, or compression must include benchmark evidence.

### Required benchmark notes in PR

- Baseline branch/commit
- Test branch/commit
- Hardware + OS summary
- .NET SDK/runtime version
- Benchmark command used
- Throughput and allocation deltas

### Performance acceptance guidance

- Regressions in hot scenarios must be justified and approved by maintainers.
- If a tradeoff is made for correctness/safety, document it explicitly.

---

## Testing Expectations

- Add or update unit tests for behavior changes.
- Include edge cases: null handling, escaping, delimiters, culture-sensitive values, and Excel compatibility mode.
- For checkpoint features, include resume and idempotency-oriented scenarios.
- For compression features, validate stream integrity and content equivalence.

---

## Pull Request Process

1. Open or reference an issue describing the change.
2. Create a focused branch (`feature/...`, `fix/...`, `perf/...`).
3. Keep commits small and logically grouped.
4. Ensure build/tests pass locally.
5. Run benchmarks for performance-sensitive changes.
6. Update docs/changelog for user-visible impact.
7. Submit PR using the provided template.

### PR review checklist

- [ ] Change is scoped and clearly explained.
- [ ] Tests added/updated and passing.
- [ ] Benchmarks included for performance-sensitive modifications.
- [ ] Documentation updated (README/docs/inline XML docs).
- [ ] Backward compatibility considerations addressed.

Thank you for building CsvForge with us.
