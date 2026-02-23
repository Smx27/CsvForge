using CsvForge;
using System.IO.Compression;
using System.Text;

var rows = Enumerable.Range(1, 5)
    .Select(i => new CompressionRow { Id = i, Name = $"row-{i}" })
    .ToArray();

var gzipPath = Path.Combine(AppContext.BaseDirectory, "compression-sample.csv.gz");
await CsvWriter.WriteToFileAsync(rows, gzipPath, new CsvOptions
{
    Compression = CsvCompressionMode.Gzip,
    EnableRuntimeMetadataFallback = true
});

await using var gzipFile = File.OpenRead(gzipPath);
await using var gzipReader = new GZipStream(gzipFile, CompressionMode.Decompress);
using var textReader = new StreamReader(gzipReader, Encoding.UTF8);
var gzipCsv = await textReader.ReadToEndAsync();

await using var streamCompressedCsv = new MemoryStream();
await using (var customGzip = new GZipStream(streamCompressedCsv, CompressionLevel.Optimal, leaveOpen: true))
{
    await CsvWriter.WriteAsync(rows, customGzip, new CsvOptions { EnableRuntimeMetadataFallback = true });
}

streamCompressedCsv.Position = 0;
await using var verifyReader = new GZipStream(streamCompressedCsv, CompressionMode.Decompress);
using var verifyText = new StreamReader(verifyReader, Encoding.UTF8);
var streamCsv = await verifyText.ReadToEndAsync();

Console.WriteLine($"Built-in gzip file written to: {gzipPath}");
Console.WriteLine("Decompressed built-in gzip payload:");
Console.WriteLine(gzipCsv);
Console.WriteLine("Decompressed stream-based gzip payload:");
Console.WriteLine(streamCsv);

public sealed class CompressionRow
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;
}
