using System.IO;
using System.Linq;
using System.Reflection;
using CsvForge.SourceGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CsvForge.Tests;

public sealed class CsvSerializableGeneratorTests
{
    [Fact]
    public void GeneratesExpectedWriterForAnnotatedType()
    {
        var input = """
using CsvForge.Attributes;

namespace Demo;

[CsvSerializable]
public partial record Order([property: CsvForge.Attributes.CsvColumn(\"id\", Order = 0)] int Id, string? CustomerName);
""";

        var result = RunGenerator(input);
        var generated = result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith("Order_CsvWriter.g.cs", System.StringComparison.Ordinal));
        var generatedText = generated.GetText().ToString().Trim();
        var expectedText = File.ReadAllText(Path.Combine(GetProjectRoot(), "tests", "CsvForge.Tests", "GoldenFiles", "Order_CsvWriter.g.cs")).Trim();

        Assert.Equal(expectedText, generatedText);
    }

    [Fact]
    public void ReportsDeterministicDiagnosticForUnsupportedIndexer()
    {
        var input = """
using CsvForge.Attributes;

namespace Demo;

[CsvSerializable]
public partial class BadType
{
    public int this[int i] => i;
}
""";

        var result = RunGenerator(input);
        var diagnostic = Assert.Single(result.Diagnostics.Where(static d => d.Id == "CSVGEN001"));
        Assert.Contains("Item", diagnostic.GetMessage());
        Assert.Contains("Demo.BadType", diagnostic.GetMessage());
    }

    private static GeneratorDriverRunResult RunGenerator(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(CsvForge.Attributes.CsvSerializableAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new CsvSerializableGenerator());
        driver = driver.RunGenerators(compilation);

        return driver.GetRunResult();
    }

    private static string GetProjectRoot()
    {
        var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "CsvForge.sln")))
            {
                return directory;
            }

            directory = Path.GetDirectoryName(directory)!;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
