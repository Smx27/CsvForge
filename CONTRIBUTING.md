# Contributing to CsvForge

Thanks for your interest in contributing to CsvForge. This guide covers local setup, coding expectations, docs/samples updates, performance validation, and pull request quality checks.

## Local development setup

1. Ensure you have a current .NET SDK installed (recommended: latest stable LTS).
2. Restore dependencies:

   ```bash
   dotnet restore
   ```

3. Build the solution:

   ```bash
   dotnet build
   ```

4. Run tests:

   ```bash
   dotnet test
   ```

Use the solution root as your working directory unless you are validating a specific project in isolation.

## Coding style expectations

- Prefer clear, readable implementations over clever but hard-to-maintain logic.
- Keep allocations and hot-path overhead in mind, especially in writer and formatter internals.
- Follow existing naming and file-organization conventions in the surrounding code.
- Keep public API changes intentional and documented.
- Add or update tests when behavior changes.
- Avoid introducing unnecessary dependencies.

If your change touches serialization behavior, generated output, or options handling, include targeted coverage in `tests/CsvForge.Tests`.

## Updating docs and samples

When you add or change user-facing behavior:

- Update the relevant documentation under `docs/` (for example: basic usage, advanced usage, performance, or FAQ).
- Update or add runnable sample projects under `samples/` when practical.
- Ensure README links and examples remain accurate.
- Keep examples minimal, compilable, and aligned with current API guidance.

## Performance-sensitive changes

For changes that may affect throughput, allocations, or memory usage:

1. Run benchmark validation with Release configuration:

   ```bash
   dotnet run -c Release --project benchmarks/CsvForge.Benchmarks/CsvForge.Benchmarks.csproj
   ```

2. Compare before/after results for representative scenarios.
3. Note significant deltas in the PR description (especially regressions and tradeoffs).
4. If benchmarks are not possible in your environment, explain why and provide the best available evidence (profiling traces, focused micro-measurements, or test-based indicators).

## Pull request checklist

Before requesting review, confirm:

- [ ] `dotnet build` and `dotnet test` pass locally.
- [ ] Documentation is updated for any user-visible behavior changes.
- [ ] Samples are added/updated when they improve discoverability or clarity.
- [ ] Performance-sensitive changes include benchmark validation details.
- [ ] Release/changelog notes are captured in the PR description for noteworthy changes.
- [ ] The PR description clearly explains motivation, approach, and any compatibility impact.

Thank you for helping improve CsvForge.
