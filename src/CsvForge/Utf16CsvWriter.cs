using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CsvForge;

internal static class Utf16CsvWriter
{
    public static void Write<T>(IEnumerable<T> data, TextWriter textWriter, CsvOptions options)
    {
        using var writer = new Utf16BufferTextWriter(textWriter);
        CsvSerializer.Write(data, writer, options);
        writer.Flush();
    }

    public static async Task WriteAsync<T>(IEnumerable<T> data, TextWriter textWriter, CsvOptions options, CancellationToken cancellationToken)
    {
        await using var writer = new Utf16BufferTextWriter(textWriter);
        await CsvSerializer.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    public static async Task WriteAsync<T>(IAsyncEnumerable<T> data, TextWriter textWriter, CsvOptions options, CancellationToken cancellationToken)
    {
        await using var writer = new Utf16BufferTextWriter(textWriter);
        await CsvSerializer.WriteAsync(data, writer, options, cancellationToken).ConfigureAwait(false);
        await writer.FlushAsync().ConfigureAwait(false);
    }

    private sealed class Utf16BufferTextWriter : TextWriter
    {
        private readonly TextWriter _inner;
        private readonly CsvCharBuffer _buffer;

        public Utf16BufferTextWriter(TextWriter inner)
        {
            _inner = inner;
            _buffer = new CsvCharBuffer(inner);
        }

        public override Encoding Encoding => _inner.Encoding;

        public override void Flush() => _inner.Flush();

        public override Task FlushAsync() => _inner.FlushAsync();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Flush();
            }
        }

        public override ValueTask DisposeAsync()
        {
            return new ValueTask(_inner.FlushAsync());
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
    }
}
