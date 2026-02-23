using CsvForge;
using CsvForge.Samples.Shared;

var generator = new SampleDataGenerator(seed: 202501);

using var generatedWriter = new StringWriter();
CsvWriter.Write(generator.GenerateGeneratedRows(count: 5), generatedWriter, new CsvOptions
{
    IncludeHeader = true,
    EnableRuntimeMetadataFallback = false
});

using var fallbackWriter = new StringWriter();
CsvWriter.Write(generator.GenerateFallbackRows(count: 5), fallbackWriter, new CsvOptions
{
    IncludeHeader = true,
    EnableRuntimeMetadataFallback = true
});

var asyncRows = new List<GeneratedSampleRow>();
await foreach (var row in generator.GenerateGeneratedRowsAsync(count: 3))
{
    asyncRows.Add(row);
}

var previewCount = 0;
foreach (var _ in generator.GenerateLargeDataset(100_000).Take(10))
{
    previewCount++;
}

Console.WriteLine("Source-generated serializer output:");
Console.WriteLine(generatedWriter.ToString());
Console.WriteLine("Runtime fallback serializer output:");
Console.WriteLine(fallbackWriter.ToString());
Console.WriteLine($"Async sample rows generated: {asyncRows.Count}");
Console.WriteLine($"Large dataset iterator preview rows generated without full materialization: {previewCount}");
