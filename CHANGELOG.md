# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-02-23

### Core

- Introduced CsvForge as a production-ready .NET 11 CSV engine.
- Added hybrid UTF-8/UTF-16 writer architecture for adaptable high-throughput export pipelines.
- Delivered Excel compatibility mode for practical interoperability with spreadsheet workflows.
- Shipped robust writer options for headers, formatting behavior, and output controls.

### Performance

- Implemented low-allocation writing internals optimized for high-volume exports.
- Added Roslyn Source Generator support for compile-time serializers with zero reflection runtime paths.
- Enabled NativeAOT-friendly serialization strategy with trimming-safe patterns.
- Added benchmark suite for repeatable throughput and allocation tracking.

### Enterprise Features

- Added checkpointed batch export coordination to support resumable long-running jobs.
- Added streaming compression support for Gzip and Zip workflows.
- Published guided samples and docs for batch, compression, Excel, source generation, and AOT scenarios.
- Established contribution, governance, and quality templates for long-term OSS sustainability.

---

## Versioning Policy

CsvForge uses semantic versioning:

- **MAJOR**: breaking API or behavior changes.
- **MINOR**: backward-compatible features.
- **PATCH**: backward-compatible bug fixes and performance fixes.
