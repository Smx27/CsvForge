using System;

namespace CsvForge;

/// <summary>
/// Specifies the strategy for handling temporary checkpoint files.
/// </summary>
public enum CsvCheckpointTempFileStrategy
{
    /// <summary>
    /// Replaces the target file.
    /// </summary>
    Replace,

    /// <summary>
    /// Moves the temporary file to the target location.
    /// </summary>
    Move
}

/// <summary>
/// Provides options for checkpoint-enabled CSV writing.
/// </summary>
public sealed class CsvCheckpointOptions
{
    /// <summary>
    /// Gets or sets the number of items to write before flushing and updating the checkpoint.
    /// </summary>
    public int BatchSize { get; init; } = 1000;

    /// <summary>
    /// Gets or sets the path to the checkpoint tracking file.
    /// </summary>
    public string CheckpointFilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to resume from an existing checkpoint if one exists.
    /// </summary>
    public bool ResumeIfExists { get; init; } = true;

    /// <summary>
    /// Gets or sets the time interval between flushes.
    /// </summary>
    public TimeSpan FlushInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the strategy for handling temporary files.
    /// </summary>
    public CsvCheckpointTempFileStrategy TempFileStrategy { get; init; } = CsvCheckpointTempFileStrategy.Replace;

    /// <summary>
    /// Gets or sets the underlying CSV writing options.
    /// </summary>
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
