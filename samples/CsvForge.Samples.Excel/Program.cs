using CsvForge;
using System.Globalization;
using System.Text;

var rows = new[]
{
    new ExcelRow { Product = "Widget", Price = 12.34m, Notes = "Line1\nLine2" },
    new ExcelRow { Product = "Cable", Price = 9.99m, Notes = "Quoted \"value\"" }
};

var excelPath = Path.Combine(AppContext.BaseDirectory, "excel-compatible.csv");
await CsvWriter.WriteToFileAsync(rows, excelPath, new CsvOptions
{
    ExcelCompatibility = true,
    FormatProvider = CultureInfo.GetCultureInfo("fr-FR"),
    EnableRuntimeMetadataFallback = true
});

var bytes = await File.ReadAllBytesAsync(excelPath);
var hasBom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
var text = Encoding.UTF8.GetString(bytes);

Console.WriteLine($"Excel-compatible export: {excelPath}");
Console.WriteLine($"UTF-8 BOM emitted: {hasBom}");
Console.WriteLine("CSV payload:");
Console.WriteLine(text);

public sealed class ExcelRow
{
    public string Product { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public string Notes { get; init; } = string.Empty;
}
