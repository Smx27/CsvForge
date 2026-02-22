using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsvForge.Checkpoint;

namespace CsvForge;

public static class CsvWriter
{
    /// <summary>
    /// Writes rows to a file at <paramref name="filePath"/>.
    /// </summary>
    public static void WriteToFile<T>(IEnumerable<T> data, string filePath, CsvOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: true);

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, options.BufferSize, useAsync: false);
        Write(data, stream, options);
    }

    /// <summary>
    /// Writes rows to a file.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="WriteToFile{T}(IEnumerable{T}, string, CsvOptions?)"/> for path-based output.
    /// </remarks>
    public static void Write<T>(IEnumerable<T> data, string path, CsvOptions? options = null)
    {
        WriteToFile(data, path, options);
    }

    public static void Write<T>(IEnumerable<T> data, Stream stream, CsvOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: true);

        using var output = CreateOutputStream(stream, options, useAsync: false);
        WriteToPreparedStream(data, output.Stream, options);
    }

    public static void Write<T>(IEnumerable<T> data, TextWriter writer, CsvOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: false);

        if (CsvEngineSelector.Select(writer) == CsvEngine.Utf16)
        {
            Utf16CsvWriter.Write(data, writer, options);
            return;
        }

        CsvSerializer.Write(data, writer, options);
        writer.Flush();
    }

    public static void Write<T>(IEnumerable<T> data, IBufferWriter<byte> bufferWriter, CsvOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(bufferWriter);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: false);
        Utf8CsvWriter.Write(data, bufferWriter, options);
    }

    public static void Write<T>(IAsyncEnumerable<T> data, IBufferWriter<byte> bufferWriter, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bufferWriter);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: false);
        Utf8CsvWriter.WriteAsync(data, bufferWriter, options, cancellationToken).GetAwaiter().GetResult();
    }

    public static void Write<T>(IEnumerable<T> data, PipeWriter writer, CsvOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: false);
        Utf8CsvWriter.Write(data, writer, options);
        writer.FlushAsync().GetAwaiter().GetResult();
    }

    public static void Write<T>(IAsyncEnumerable<T> data, PipeWriter writer, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: false);
        Utf8CsvWriter.WriteAsync(data, writer, options, cancellationToken).GetAwaiter().GetResult();
        writer.FlushAsync(cancellationToken).GetAwaiter().GetResult();
    }


    public static async Task WriteWithCheckpointAsync<T>(IAsyncEnumerable<T> data, string path, CsvCheckpointOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        var csvOptions = CsvOptions.NormalizeForWrite(options.CsvOptions ?? CsvOptions.Default, streamOrFileTarget: true);
        var coordinator = new CsvCheckpointCoordinator(options.CheckpointFilePath, options.TempFileStrategy);
        var checkpointIndex = options.ResumeIfExists ? await coordinator.LoadAsync(CancellationToken.None).ConfigureAwait(false) : -1;

        var includeHeader = csvOptions.IncludeHeader && !(options.ResumeIfExists && checkpointIndex >= 0 && File.Exists(path));
        var writeOptions = csvOptions with { IncludeHeader = includeHeader };
        var fileMode = options.ResumeIfExists && checkpointIndex >= 0 && File.Exists(path) ? FileMode.Append : FileMode.Create;

        await using var stream = new FileStream(path, fileMode, FileAccess.Write, FileShare.Read, csvOptions.BufferSize, useAsync: true);

        var batch = new List<T>(options.BatchSize);
        var nextRowIndex = 0L;
        var lastFlushUtc = DateTime.UtcNow;

        await foreach (var item in data.ConfigureAwait(false))
        {
            if (nextRowIndex <= checkpointIndex)
            {
                nextRowIndex++;
                continue;
            }

            batch.Add(item);
            nextRowIndex++;

            if (batch.Count >= options.BatchSize || (options.FlushInterval > TimeSpan.Zero && DateTime.UtcNow - lastFlushUtc >= options.FlushInterval))
            {
                await FlushBatchAsync(batch, stream, writeOptions).ConfigureAwait(false);
                await coordinator.PersistAsync(nextRowIndex - 1, CancellationToken.None).ConfigureAwait(false);
                lastFlushUtc = DateTime.UtcNow;
                writeOptions = writeOptions with { IncludeHeader = false };
            }
        }

        if (batch.Count > 0)
        {
            await FlushBatchAsync(batch, stream, writeOptions).ConfigureAwait(false);
            await coordinator.PersistAsync(nextRowIndex - 1, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static async Task FlushBatchAsync<T>(List<T> batch, Stream stream, CsvOptions options)
    {
        if (CsvEngineSelector.Select(stream) == CsvEngine.Utf8)
        {
            var bufferWriter = new StreamBufferWriter(stream);
            if (stream.CanSeek && stream.Position == 0)
            {
                WriteUtf8BomIfNeeded(bufferWriter, options);
            }

            await Utf8CsvWriter.WriteAsync(batch, bufferWriter, options, CancellationToken.None).ConfigureAwait(false);
            await bufferWriter.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }
        else
        {
            await using var writer = new StreamWriter(stream, options.Encoding, options.StreamWriterBufferSize, leaveOpen: true);
            await Utf16CsvWriter.WriteAsync(batch, writer, options, CancellationToken.None).ConfigureAwait(false);
            await writer.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }

        batch.Clear();
    }

    public static Task WriteToFileAsync<T>(IEnumerable<T> data, string filePath, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        return WriteEnumerableToPathAsync(data, filePath, options, cancellationToken);
    }

    public static Task WriteToFileAsync<T>(IAsyncEnumerable<T> data, string filePath, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        return WriteAsyncEnumerableToPathAsync(data, filePath, options, cancellationToken);
    }

    public static Task WriteAsync<T>(IEnumerable<T> data, string path, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        return WriteToFileAsync(data, path, options, cancellationToken);
    }

    public static Task WriteAsync<T>(IEnumerable<T> data, Stream stream, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        return WriteEnumerableToStreamAsync(data, stream, options, cancellationToken);
    }

    public static Task WriteAsync<T>(IEnumerable<T> data, TextWriter writer, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: false);
        return Utf16CsvWriter.WriteAsync(data, writer, options, cancellationToken);
    }

    public static Task WriteAsync<T>(IEnumerable<T> data, IBufferWriter<byte> bufferWriter, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bufferWriter);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: false);
        return Utf8CsvWriter.WriteAsync(data, bufferWriter, options, cancellationToken);
    }

    public static async Task WriteAsync<T>(IEnumerable<T> data, PipeWriter writer, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: false);
        await Utf8CsvWriter.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static Task WriteAsync<T>(IAsyncEnumerable<T> data, string path, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        return WriteToFileAsync(data, path, options, cancellationToken);
    }

    public static Task WriteAsync<T>(IAsyncEnumerable<T> data, Stream stream, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        return WriteAsyncEnumerableToStreamAsync(data, stream, options, cancellationToken);
    }

    public static Task WriteAsync<T>(IAsyncEnumerable<T> data, TextWriter writer, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: false);
        return Utf16CsvWriter.WriteAsync(data, writer, options, cancellationToken);
    }

    public static Task WriteAsync<T>(IAsyncEnumerable<T> data, IBufferWriter<byte> bufferWriter, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bufferWriter);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: false);
        return Utf8CsvWriter.WriteAsync(data, bufferWriter, options, cancellationToken);
    }

    public static async Task WriteAsync<T>(IAsyncEnumerable<T> data, PipeWriter writer, CsvOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writer);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: false);
        await Utf8CsvWriter.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteEnumerableToPathAsync<T>(IEnumerable<T> data, string path, CsvOptions? options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: true);

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, options.BufferSize, useAsync: true);
        await WriteAsync(data, stream, options, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteEnumerableToStreamAsync<T>(IEnumerable<T> data, Stream stream, CsvOptions? options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: true);

        await using var output = CreateOutputStream(stream, options, useAsync: true);
        await WriteToPreparedStreamAsync(data, output.Stream, options, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteAsyncEnumerableToPathAsync<T>(IAsyncEnumerable<T> data, string path, CsvOptions? options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(path);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: true);

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, options.BufferSize, useAsync: true);
        await WriteAsync(data, stream, options, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteAsyncEnumerableToStreamAsync<T>(IAsyncEnumerable<T> data, Stream stream, CsvOptions? options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        options ??= CsvOptions.Default;
        options = CsvOptions.NormalizeForWrite(options, streamOrFileTarget: true);

        await using var output = CreateOutputStream(stream, options, useAsync: true);
        await WriteToPreparedStreamAsync(data, output.Stream, options, cancellationToken).ConfigureAwait(false);
    }


    private static void WriteToPreparedStream<T>(IEnumerable<T> data, Stream stream, CsvOptions options)
    {
        if (CsvEngineSelector.Select(stream) == CsvEngine.Utf8)
        {
            var bufferWriter = new StreamBufferWriter(stream);
            WriteUtf8BomIfNeeded(bufferWriter, options);
            Utf8CsvWriter.Write(data, bufferWriter, options);
            bufferWriter.Flush();
            return;
        }

        using var writer = new StreamWriter(stream, options.Encoding, options.StreamWriterBufferSize, leaveOpen: true);
        Utf16CsvWriter.Write(data, writer, options);
    }

    private static async Task WriteToPreparedStreamAsync<T>(IEnumerable<T> data, Stream stream, CsvOptions options, CancellationToken cancellationToken)
    {
        if (CsvEngineSelector.Select(stream) == CsvEngine.Utf8)
        {
            var bufferWriter = new StreamBufferWriter(stream);
            WriteUtf8BomIfNeeded(bufferWriter, options);
            await Utf8CsvWriter.WriteAsync(data, bufferWriter, options, cancellationToken).ConfigureAwait(false);
            await bufferWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await using var writer = new StreamWriter(stream, options.Encoding, options.StreamWriterBufferSize, leaveOpen: true);
        await Utf16CsvWriter.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteToPreparedStreamAsync<T>(IAsyncEnumerable<T> data, Stream stream, CsvOptions options, CancellationToken cancellationToken)
    {
        if (CsvEngineSelector.Select(stream) == CsvEngine.Utf8)
        {
            var bufferWriter = new StreamBufferWriter(stream);
            WriteUtf8BomIfNeeded(bufferWriter, options);
            await Utf8CsvWriter.WriteAsync(data, bufferWriter, options, cancellationToken).ConfigureAwait(false);
            await bufferWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        await using var writer = new StreamWriter(stream, options.Encoding, options.StreamWriterBufferSize, leaveOpen: true);
        await Utf16CsvWriter.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
    }


    private static void WriteUtf8BomIfNeeded(System.Buffers.IBufferWriter<byte> writer, CsvOptions options)
    {
        if (!options.EmitUtf8Bom || options.Encoding.CodePage != Encoding.UTF8.CodePage)
        {
            return;
        }

        var preamble = options.Encoding.GetPreamble();
        if (preamble.Length == 0)
        {
            return;
        }

        var span = writer.GetSpan(preamble.Length);
        preamble.CopyTo(span);
        writer.Advance(preamble.Length);
    }

    private static OutputStreamContext CreateOutputStream(Stream destination, CsvOptions options, bool useAsync)
    {
        return options.Compression switch
        {
            CsvCompressionMode.None => new OutputStreamContext(destination, disposable: null, asyncDisposable: null),
            CsvCompressionMode.Gzip => CreateGzipOutputStream(destination, useAsync),
            CsvCompressionMode.Zip => CreateZipOutputStream(destination),
            _ => throw new ArgumentOutOfRangeException(nameof(options.Compression), options.Compression, "Unsupported compression mode.")
        };
    }

    private static OutputStreamContext CreateGzipOutputStream(Stream destination, bool useAsync)
    {
        var gzip = new GZipStream(destination, CompressionLevel.Optimal, leaveOpen: true);
        return useAsync
            ? new OutputStreamContext(gzip, disposable: null, asyncDisposable: gzip)
            : new OutputStreamContext(gzip, disposable: gzip, asyncDisposable: null);
    }

    private static OutputStreamContext CreateZipOutputStream(Stream destination)
    {
        var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
        var entry = archive.CreateEntry("data.csv", CompressionLevel.Optimal);
        var entryStream = entry.Open();
        return new OutputStreamContext(entryStream, new ZipOutputScope(entryStream, archive), asyncDisposable: null);
    }

    private sealed class OutputStreamContext : IDisposable, IAsyncDisposable
    {
        private readonly IDisposable? _disposable;
        private readonly IAsyncDisposable? _asyncDisposable;

        public OutputStreamContext(Stream stream, IDisposable? disposable, IAsyncDisposable? asyncDisposable)
        {
            Stream = stream;
            _disposable = disposable;
            _asyncDisposable = asyncDisposable;
        }

        public Stream Stream { get; }

        public void Dispose()
        {
            _disposable?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_asyncDisposable is not null)
            {
                await _asyncDisposable.DisposeAsync().ConfigureAwait(false);
                return;
            }

            _disposable?.Dispose();
        }
    }

    private sealed class ZipOutputScope : IDisposable
    {
        private readonly Stream _entryStream;
        private readonly ZipArchive _archive;

        public ZipOutputScope(Stream entryStream, ZipArchive archive)
        {
            _entryStream = entryStream;
            _archive = archive;
        }

        public void Dispose()
        {
            _entryStream.Dispose();
            _archive.Dispose();
        }
    }

    private sealed class StreamBufferWriter : IBufferWriter<byte>
    {
        private readonly Stream _stream;
        private byte[] _buffer;
        private int _written;

        public StreamBufferWriter(Stream stream)
        {
            _stream = stream;
            _buffer = new byte[16 * 1024];
        }

        public void Advance(int count)
        {
            _written += count;
            if (_written >= _buffer.Length)
            {
                Flush();
            }
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            Ensure(sizeHint);
            return _buffer.AsMemory(_written);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            Ensure(sizeHint);
            return _buffer.AsSpan(_written);
        }

        public void Flush()
        {
            if (_written == 0)
            {
                return;
            }

            _stream.Write(_buffer, 0, _written);
            _written = 0;
        }

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_written == 0)
            {
                return;
            }

            await _stream.WriteAsync(_buffer.AsMemory(0, _written), cancellationToken).ConfigureAwait(false);
            _written = 0;
        }

        private void Ensure(int sizeHint)
        {
            sizeHint = Math.Max(1, sizeHint);

            if (_buffer.Length - _written >= sizeHint)
            {
                return;
            }

            Flush();
            if (_buffer.Length >= sizeHint)
            {
                return;
            }

            _buffer = new byte[Math.Max(_buffer.Length * 2, sizeHint)];
        }
    }
}
