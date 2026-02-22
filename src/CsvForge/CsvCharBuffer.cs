using System;
using System.IO;
using System.Threading.Tasks;

namespace CsvForge;

internal sealed class CsvCharBuffer
{
    private readonly TextWriter _writer;

    public CsvCharBuffer(TextWriter writer)
    {
        _writer = writer;
    }

    public void Write(ReadOnlySpan<char> value)
    {
        _writer.Write(value);
    }

    public void Write(char value)
    {
        _writer.Write(value);
    }

    public ValueTask FlushAsync()
    {
        return new ValueTask(_writer.FlushAsync());
    }
}
