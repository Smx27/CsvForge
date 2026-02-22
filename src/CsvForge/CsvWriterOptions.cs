namespace CsvForge;

public sealed record CsvWriterOptions(char Delimiter = ',', bool IncludeHeader = true, string NewLine = "\n");
