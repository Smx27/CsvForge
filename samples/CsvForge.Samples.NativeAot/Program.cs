using CsvForge;
using CsvForge.Attributes;

var rows = new[]
{
    new NativeAotOrder { Id = 1, Customer = "Ada", Total = 41.25m },
    new NativeAotOrder { Id = 2, Customer = "Grace", Total = 99.99m }
};

using var writer = new StringWriter();
CsvWriter.Write(rows, writer, new CsvOptions
{
    StrictMode = true,
    EnableRuntimeMetadataFallback = false
});

Console.WriteLine("NativeAOT-friendly CSV output (source-generator-first, reflection-free path):");
Console.WriteLine(writer.ToString());
Console.WriteLine("Publish with:");
Console.WriteLine("dotnet publish samples/CsvForge.Samples.NativeAot/CsvForge.Samples.NativeAot.csproj -c Release -r linux-x64");

[CsvSerializable]
public partial class NativeAotOrder
{
    public int Id { get; init; }

    public string Customer { get; init; } = string.Empty;

    public decimal Total { get; init; }
}
