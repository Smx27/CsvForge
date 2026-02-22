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
    public void GeneratesExpectedWritersForAnnotatedType()
    {
        var input = """
using CsvForge.Attributes;

namespace Demo;

[CsvSerializable]
public partial record Order([property: CsvForge.Attributes.CsvColumn(\"id\", Order = 0)] int Id, string? CustomerName);
""";

        var result = RunGenerator(input);
        var utf16Generated = result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith("Order_CsvUtf16Writer.g.cs", System.StringComparison.Ordinal));
        var utf16GeneratedText = utf16Generated.GetText().ToString().Trim();
        var utf16ExpectedText = File.ReadAllText(Path.Combine(GetProjectRoot(), "tests", "CsvForge.Tests", "GoldenFiles", "Order_CsvUtf16Writer.g.cs")).Trim();
        Assert.Equal(utf16ExpectedText, utf16GeneratedText);

        var utf8Generated = result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith("Order_CsvUtf8Writer.g.cs", System.StringComparison.Ordinal));
        var utf8GeneratedText = utf8Generated.GetText().ToString().Trim();
        var utf8ExpectedText = File.ReadAllText(Path.Combine(GetProjectRoot(), "tests", "CsvForge.Tests", "GoldenFiles", "Order_CsvUtf8Writer.g.cs")).Trim();
        Assert.Equal(utf8ExpectedText, utf8GeneratedText);
    }


    [Fact]
    public void GeneratesWriterThatHonorsCsvIgnoreAndColumnOrderingTies()
    {
        var input = """
using CsvForge.Attributes;

namespace Demo;

[CsvSerializable]
public partial class Inventory
{
    [CsvColumn("z", Order = 1)]
    public int Zeta { get; set; }

    [CsvColumn("a", Order = 1)]
    public int Alpha { get; set; }

    [CsvColumn("first", Order = 0)]
    public int First { get; set; }

    [CsvIgnore]
    public int Hidden { get; set; }
}
""";

        var result = RunGenerator(input);
        var generated = result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith("Inventory_CsvUtf16Writer.g.cs", System.StringComparison.Ordinal));
        var generatedText = generated.GetText().ToString();

        Assert.Contains("first", generatedText);
        Assert.Contains("a", generatedText);
        Assert.Contains("z", generatedText);
        Assert.DoesNotContain("Hidden", generatedText);

        Assert.True(generatedText.IndexOf("first", System.StringComparison.Ordinal) < generatedText.IndexOf("a", System.StringComparison.Ordinal));
        Assert.True(generatedText.IndexOf("a", System.StringComparison.Ordinal) < generatedText.IndexOf("z", System.StringComparison.Ordinal));
    }

    [Fact]
    public void GeneratesWriterThatPrefersCsvColumnNameOverJsonPropertyName()
    {
        var input = """
using CsvForge.Attributes;
using System.Text.Json.Serialization;

namespace Demo;

[CsvSerializable]
public partial class NamePrecedence
{
    [CsvColumn("csv_name")]
    [JsonPropertyName("json_name")]
    public int Value { get; set; }

    [JsonPropertyName("json_only")]
    public int JsonOnly { get; set; }
}
""";

        var result = RunGenerator(input);
        var generated = result.GeneratedTrees.Single(tree => tree.FilePath.EndsWith("NamePrecedence_CsvUtf16Writer.g.cs", System.StringComparison.Ordinal));
        var generatedText = generated.GetText().ToString();

        Assert.Contains("csv_name", generatedText);
        Assert.Contains("json_only", generatedText);
        Assert.DoesNotContain("json_name", generatedText);
    }


    [Fact]
    public void EmitsUtf8AndUtf16WriterTypes()
    {
        var input = """
using CsvForge.Attributes;

namespace Demo;

[CsvSerializable]
public partial class DualWriter
{
    public int Id { get; set; }
}
""";

        var result = RunGenerator(input);
        Assert.Contains(result.GeneratedTrees, tree => tree.FilePath.EndsWith("DualWriter_CsvUtf8Writer.g.cs", System.StringComparison.Ordinal));
        Assert.Contains(result.GeneratedTrees, tree => tree.FilePath.EndsWith("DualWriter_CsvUtf16Writer.g.cs", System.StringComparison.Ordinal));
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
