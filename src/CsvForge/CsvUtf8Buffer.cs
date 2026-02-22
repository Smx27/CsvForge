using System;
using System.Buffers;
using System.Text;

namespace CsvForge;

internal sealed class CsvUtf8Buffer : IDisposable
{
    private readonly IBufferWriter<byte> _writer;
    private readonly Encoding _encoding;
    private char[] _scratch;

    public CsvUtf8Buffer(IBufferWriter<byte> writer, Encoding encoding)
    {
        _writer = writer;
        _encoding = encoding;
        _scratch = ArrayPool<char>.Shared.Rent(256);
    }

    public void Write(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return;
        }

        var byteCount = _encoding.GetByteCount(value);
        var destination = _writer.GetSpan(byteCount);
        var bytesWritten = _encoding.GetBytes(value, destination);
        _writer.Advance(bytesWritten);
    }

    public void Write(char value)
    {
        if (_scratch.Length < 1)
        {
            ArrayPool<char>.Shared.Return(_scratch);
            _scratch = ArrayPool<char>.Shared.Rent(1);
        }

        _scratch[0] = value;
        Write(_scratch.AsSpan(0, 1));
    }

    public ValueTask FlushAsync() => ValueTask.CompletedTask;

    public void Dispose()
    {
        ArrayPool<char>.Shared.Return(_scratch);
        _scratch = Array.Empty<char>();
    }
}
