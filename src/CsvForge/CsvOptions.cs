using System;
using System.Globalization;
using System.Text;

namespace CsvForge;

public enum CsvNewLineBehavior
{
    Environment,
    Lf,
    CrLf
}

public sealed class CsvOptions
{
    public static CsvOptions Default { get; } = new();

    public char Delimiter { get; init; } = ',';

    public Encoding Encoding { get; init; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public bool IncludeHeader { get; init; } = true;

    public int BufferSize { get; init; } = 16 * 1024;

    public int StreamWriterBufferSize { get; init; } = 16 * 1024;

    public IFormatProvider FormatProvider { get; init; } = CultureInfo.InvariantCulture;

    public CsvNewLineBehavior NewLineBehavior { get; init; } = CsvNewLineBehavior.Environment;

    internal string NewLine => NewLineBehavior switch
    {
        CsvNewLineBehavior.Lf => "\n",
        CsvNewLineBehavior.CrLf => "\r\n",
        _ => Environment.NewLine
    };
}
