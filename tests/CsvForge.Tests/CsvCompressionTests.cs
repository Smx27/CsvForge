using System.IO.Compression;
using System.Text;
using CsvForge;

namespace CsvForge.Tests;

public class CsvCompressionTests
{
    [Theory]
    [InlineData(CsvCompressionMode.None)]
    [InlineData(CsvCompressionMode.Gzip)]
    [InlineData(CsvCompressionMode.Zip)]
    public void Write_ShouldEmitParsableOutput_ForEachCompressionMode(CsvCompressionMode compression)
    {
        var rows = CreateRows(3);
        var options = CreateOptions(compression);

        using var destination = new MemoryStream();
        CsvWriter.Write(rows, destination, options);

        var csv = DecompressToCsv(destination.ToArray(), compression, options.Encoding);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Id,Name", lines[0]);
        Assert.Equal(rows.Length + 1, lines.Length);
        Assert.Equal("1,row-1", lines[1]);
        Assert.Equal($"{rows.Length},row-{rows.Length}", lines[^1]);
    }

    [Theory]
    [InlineData(CsvCompressionMode.None)]
    [InlineData(CsvCompressionMode.Gzip)]
    [InlineData(CsvCompressionMode.Zip)]
    public async Task WriteAsync_ShouldEmitParsableOutput_ForEachCompressionMode(CsvCompressionMode compression)
    {
        var rows = CreateRows(200);
        var options = CreateOptions(compression);

        using var destination = new MemoryStream();
        await CsvWriter.WriteAsync(ToAsyncEnumerable(rows), destination, options);

        var csv = DecompressToCsv(destination.ToArray(), compression, options.Encoding);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Id,Name", lines[0]);
        Assert.Equal(rows.Length + 1, lines.Length);
        Assert.Equal("1,row-1", lines[1]);
        Assert.Equal($"{rows.Length},row-{rows.Length}", lines[^1]);
    }

    [Theory]
    [InlineData(CsvCompressionMode.None)]
    [InlineData(CsvCompressionMode.Gzip)]
    [InlineData(CsvCompressionMode.Zip)]
    public async Task WriteAsync_StreamingSource_ShouldWriteIncrementally(CsvCompressionMode compression)
    {
        const int rowCount = 50_000;
        var options = CreateOptions(compression);
        await using var destination = new TrackingWriteStream();

        await CsvWriter.WriteAsync(CreateAsyncRows(rowCount), destination, options);

        Assert.True(destination.TotalBytesWritten > 200_000);
        Assert.True(destination.MaxWriteSize < destination.TotalBytesWritten / 2);

        var csv = DecompressToCsv(destination.ToArray(), compression, options.Encoding);
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(rowCount + 1, lines.Length);
        Assert.Equal("Id,Name", lines[0]);
    }

    private static CsvOptions CreateOptions(CsvCompressionMode compression) => new()
    {
        Compression = compression,
        NewLineBehavior = CsvNewLineBehavior.Lf,
        EnableRuntimeMetadataFallback = true
    };

    private static CompressionRow[] CreateRows(int count)
    {
        return Enumerable.Range(1, count)
            .Select(static index => new CompressionRow { Id = index, Name = $"row-{index}" })
            .ToArray();
    }

    private static async IAsyncEnumerable<CompressionRow> CreateAsyncRows(int count)
    {
        for (var i = 1; i <= count; i++)
        {
            if (i % 8192 == 0)
            {
                await Task.Yield();
            }

            yield return new CompressionRow { Id = i, Name = $"row-{i}" };
        }
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> rows)
    {
        foreach (var row in rows)
        {
            await Task.Yield();
            yield return row;
        }
    }

    private static string DecompressToCsv(byte[] payload, CsvCompressionMode compression, Encoding encoding)
    {
        using var source = new MemoryStream(payload);

        return compression switch
        {
            CsvCompressionMode.None => encoding.GetString(payload),
            CsvCompressionMode.Gzip => ReadAllText(new GZipStream(source, CompressionMode.Decompress), encoding),
            CsvCompressionMode.Zip => ReadZipCsv(source, encoding),
            _ => throw new ArgumentOutOfRangeException(nameof(compression), compression, "Unsupported compression mode")
        };
    }

    private static string ReadZipCsv(Stream source, Encoding encoding)
    {
        using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true);
        var entry = Assert.Single(archive.Entries);
        using var entryStream = entry.Open();
        return ReadAllText(entryStream, encoding);
    }

    private static string ReadAllText(Stream source, Encoding encoding)
    {
        using var reader = new StreamReader(source, encoding, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return reader.ReadToEnd();
    }

    private sealed class CompressionRow
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class TrackingWriteStream : MemoryStream
    {
        public int MaxWriteSize { get; private set; }

        public long TotalBytesWritten { get; private set; }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Track(count);
            base.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Track(buffer.Length);
            base.Write(buffer);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            Track(buffer.Length);
            return base.WriteAsync(buffer, cancellationToken);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Track(count);
            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        private void Track(int count)
        {
            if (count > MaxWriteSize)
            {
                MaxWriteSize = count;
            }

            TotalBytesWritten += count;
        }
    }
}
