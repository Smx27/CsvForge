using System;

namespace CsvForge;

public enum CsvCheckpointTempFileStrategy
{
    Replace,
    Move
}

public sealed class CsvCheckpointOptions
{
    public int BatchSize { get; init; } = 1000;

    public string CheckpointFilePath { get; init; } = string.Empty;

    public bool ResumeIfExists { get; init; } = true;

    public TimeSpan FlushInterval { get; init; } = TimeSpan.FromSeconds(1);

    public CsvCheckpointTempFileStrategy TempFileStrategy { get; init; } = CsvCheckpointTempFileStrategy.Replace;

    public CsvOptions? CsvOptions { get; init; }

    internal void Validate()
    {
        if (BatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(BatchSize), "BatchSize must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(CheckpointFilePath))
        {
            throw new ArgumentException("CheckpointFilePath is required.", nameof(CheckpointFilePath));
        }

        if (FlushInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(FlushInterval), "FlushInterval cannot be negative.");
        }
    }
}
