namespace CsvForge;

/// <summary>
/// Options for configuring CSV writer behavior.
/// </summary>
/// <param name="Delimiter">The character to use as a column delimiter.</param>
/// <param name="IncludeHeader">Whether to include the header row in the output.</param>
/// <param name="NewLine">The string to use for line endings.</param>
public sealed record CsvWriterOptions(char Delimiter = ',', bool IncludeHeader = true, string NewLine = "\n");
