using CsvForge;

var rows = new List<CustomerExportRow>
{
    new() { Id = 1, Name = "Ada Lovelace", Email = "ada@csvforge.dev", IsActive = true },
    new() { Id = 2, Name = "Grace Hopper", Email = "grace@csvforge.dev", IsActive = false },
    new() { Id = 3, Name = "Alan Turing", Email = "alan@csvforge.dev", IsActive = true }
};

var outputPath = Path.Combine(AppContext.BaseDirectory, "basic-export.csv");
await CsvWriter.WriteToFileAsync(rows, outputPath, new CsvOptions { EnableRuntimeMetadataFallback = true });

Console.WriteLine($"Wrote {rows.Count} rows to {outputPath}");
Console.WriteLine(await File.ReadAllTextAsync(outputPath));

public sealed class CustomerExportRow
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
