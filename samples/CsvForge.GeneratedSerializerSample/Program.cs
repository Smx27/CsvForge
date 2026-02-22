using CsvForge;
using CsvForge.Attributes;

var generatedRows = new[]
{
    new SampleOrder { Id = 1, Customer = "Ada" },
    new SampleOrder { Id = 2, Customer = "Grace" }
};

var fallbackRows = new[]
{
    new FallbackOrder { Id = 1, Customer = "Ada" },
    new FallbackOrder { Id = 2, Customer = "Grace" }
};

using var generatedWriter = new StringWriter();
CsvWriter.Write(generatedRows, generatedWriter, new CsvOptions
{
    IncludeHeader = true,
    EnableRuntimeMetadataFallback = false
});

using var fallbackWriter = new StringWriter();
CsvWriter.Write(fallbackRows, fallbackWriter, new CsvOptions
{
    IncludeHeader = true,
    EnableRuntimeMetadataFallback = true
});

Console.WriteLine("Source-generated serializer output:");
Console.WriteLine(generatedWriter.ToString());
Console.WriteLine("Runtime fallback serializer output:");
Console.WriteLine(fallbackWriter.ToString());

[CsvSerializable]
public partial class SampleOrder
{
    public int Id { get; init; }
    public string Customer { get; init; } = string.Empty;
}

public sealed class FallbackOrder
{
    public int Id { get; init; }
    public string Customer { get; init; } = string.Empty;
}
