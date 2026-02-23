using CsvForge;

var outputPath = Path.Combine(AppContext.BaseDirectory, "checkpointing-export.csv");
var checkpointPath = Path.Combine(AppContext.BaseDirectory, "checkpointing-export.chk");

if (File.Exists(outputPath))
{
    File.Delete(outputPath);
}

if (File.Exists(checkpointPath))
{
    File.Delete(checkpointPath);
}

var checkpointOptions = new CsvCheckpointOptions
{
    BatchSize = 500,
    CheckpointFilePath = checkpointPath,
    ResumeIfExists = true,
    FlushInterval = TimeSpan.Zero,
    CsvOptions = new CsvOptions
    {
        NewLineBehavior = CsvNewLineBehavior.Lf,
        EnableRuntimeMetadataFallback = true
    }
};

try
{
    await CsvWriter.WriteWithCheckpointAsync(GenerateRows(totalRows: 5_000, throwAfter: 2_200), outputPath, checkpointOptions);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Simulated failure captured: {ex.Message}");
}

Console.WriteLine($"Checkpoint after failure: {await File.ReadAllTextAsync(checkpointPath)}");

await CsvWriter.WriteWithCheckpointAsync(GenerateRows(totalRows: 5_000), outputPath, checkpointOptions);

var lineCount = File.ReadLines(outputPath).Count();
Console.WriteLine($"Resume complete. CSV line count (including header): {lineCount}");
Console.WriteLine($"Final checkpoint: {await File.ReadAllTextAsync(checkpointPath)}");

static async IAsyncEnumerable<CheckpointRow> GenerateRows(int totalRows, int? throwAfter = null)
{
    for (var i = 0; i < totalRows; i++)
    {
        if (throwAfter.HasValue && i == throwAfter)
        {
            throw new InvalidOperationException($"Failure injected after row {throwAfter.Value}.");
        }

        yield return new CheckpointRow
        {
            Sequence = i,
            Payload = $"value-{i:D6}"
        };

        await Task.Yield();
    }
}

public sealed class CheckpointRow
{
    public int Sequence { get; init; }

    public string Payload { get; init; } = string.Empty;
}
