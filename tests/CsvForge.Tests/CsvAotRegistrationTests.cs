using System;
using System.Buffers;
using System.IO;
using System.Reflection;
using System.Text;
using CsvForge.Attributes;
using Xunit;

namespace CsvForge.Tests;

public sealed class CsvAotRegistrationTests
{
    [Fact]
    public void GeneratedUtf16Writer_ShouldResolveWithoutRuntimeFallback()
    {
        var rows = new[] { new AotOrder { Id = 42, Name = "Ada" } };
        using var writer = new StringWriter();

        CsvWriter.Write(rows, writer, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf });

        Assert.Equal("Id,Name\n42,Ada\n", writer.ToString());
    }

    [Fact]
    public void GeneratedUtf8Writer_ShouldResolveWithoutRuntimeFallback()
    {
        var rows = new[] { new AotOrder { Id = 42, Name = "Ada" } };
        var buffer = new ArrayBufferWriter<byte>();

        CsvWriter.Write(rows, buffer, new CsvOptions { NewLineBehavior = CsvNewLineBehavior.Lf });

        Assert.Equal("Id,Name\n42,Ada\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    [Fact]
    public void TypeWriterCaches_ShouldNotDependOnAssemblyScanningOrLateBinding()
    {
        var root = GetProjectRoot();
        var utf16Cache = File.ReadAllText(Path.Combine(root, "src", "CsvForge", "CsvTypeWriterCache.cs"));
        var utf8Cache = File.ReadAllText(Path.Combine(root, "src", "CsvForge", "CsvUtf8TypeWriterCache.cs"));

        Assert.DoesNotContain("GetTypes", utf16Cache, StringComparison.Ordinal);
        Assert.DoesNotContain("Activator", utf16Cache, StringComparison.Ordinal);
        Assert.DoesNotContain("GetField", utf16Cache, StringComparison.Ordinal);

        Assert.DoesNotContain("GetTypes", utf8Cache, StringComparison.Ordinal);
        Assert.DoesNotContain("Activator", utf8Cache, StringComparison.Ordinal);
        Assert.DoesNotContain("GetField", utf8Cache, StringComparison.Ordinal);
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

    [CsvSerializable]
    public partial class AotOrder
    {
        public int Id { get; set; }

        public string? Name { get; set; }
    }
}
