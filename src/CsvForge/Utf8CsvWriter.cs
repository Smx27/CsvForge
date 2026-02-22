using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CsvForge;

internal static class Utf8CsvWriter
{
    public static void Write<T>(IEnumerable<T> data, IBufferWriter<byte> bufferWriter, CsvOptions options)
    {
        using var writer = new Utf8BufferTextWriter(bufferWriter, options.Encoding);
        CsvSerializer.Write(data, writer, options);
        writer.Flush();
    }

    public static async Task WriteAsync<T>(IEnumerable<T> data, IBufferWriter<byte> bufferWriter, CsvOptions options, CancellationToken cancellationToken)
    {
        await using var writer = new Utf8BufferTextWriter(bufferWriter, options.Encoding);
        await CsvSerializer.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    public static async Task WriteAsync<T>(IAsyncEnumerable<T> data, IBufferWriter<byte> bufferWriter, CsvOptions options, CancellationToken cancellationToken)
    {
        await using var writer = new Utf8BufferTextWriter(bufferWriter, options.Encoding);
        await CsvSerializer.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    private sealed class Utf8BufferTextWriter : TextWriter
    {
        private readonly CsvUtf8Buffer _buffer;

        public Utf8BufferTextWriter(IBufferWriter<byte> writer, Encoding encoding)
        {
            _buffer = new CsvUtf8Buffer(writer, encoding);
            Encoding = encoding;
        }

        public override Encoding Encoding { get; }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _buffer.Dispose();
            }

            base.Dispose(disposing);
        }

        public override ValueTask DisposeAsync()
        {
            _buffer.Dispose();
            return ValueTask.CompletedTask;
        }

        public override void Write(char value) => _buffer.Write(value);

        public override void Write(ReadOnlySpan<char> buffer) => _buffer.Write(buffer);

        public override void Write(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            _buffer.Write(value.AsSpan());
        }

        public override Task WriteAsync(char value)
        {
            _buffer.Write(value);
            return Task.CompletedTask;
        }

        public override Task WriteAsync(string? value)
        {
            Write(value);
            return Task.CompletedTask;
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            _buffer.Write(buffer.AsSpan(index, count));
            return Task.CompletedTask;
        }

        public override Task FlushAsync() => Task.CompletedTask;
    }
}
