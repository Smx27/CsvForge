# Using CsvForge from NuGet Packages

The sample projects in this directory are configured to use local project references for development. However, when using CsvForge in your own projects, you should install from NuGet.org.

## Installation

Install the main package:
```bash
dotnet add package CsvForge
```

For projects using the Roslyn source generator (recommended for performance-critical code):
```bash
dotnet add package CsvForge.SourceGenerator
```

## Example Project Files

### Basic Usage (Project File)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CsvForge" Version="1.0.0" />
  </ItemGroup>
</Project>
```

### Advanced Usage with Source Generator
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="CsvForge" Version="1.0.0" />
    <PackageReference Include="CsvForge.SourceGenerator" Version="1.0.0" />
  </ItemGroup>
</Project>
```

## Package Details

- **CsvForge** (1.0.0): Main CSV serialization library
  - High-performance UTF-8/UTF-16 writer
  - NativeAOT compatible
  - Trimming safe
  - Compression support (Gzip/Zip)
  - Checkpoint/resume for long-running exports

- **CsvForge.SourceGenerator** (1.0.0): Roslyn source generator (optional)
  - Compile-time serializer generation
  - Zero-reflection runtime path
  - Better performance for typed exports
  - Marked as DevelopmentDependency

## Package Links

- CsvForge: https://www.nuget.org/packages/CsvForge
- CsvForge.SourceGenerator: https://www.nuget.org/packages/CsvForge.SourceGenerator

## More Information

For detailed usage examples, see the sample projects in this directory (they use local project references for development, but the code is identical to what you'd write using the NuGet packages).
