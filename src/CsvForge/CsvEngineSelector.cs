using System.Buffers;
using System.IO;
using System.IO.Pipelines;

namespace CsvForge;

internal enum CsvEngine
{
    Utf8,
    Utf16
}

internal static class CsvEngineSelector
{
    public static CsvEngine Select(Stream _) => CsvEngine.Utf8;

    public static CsvEngine Select(IBufferWriter<byte> _) => CsvEngine.Utf8;

    public static CsvEngine Select(PipeWriter _) => CsvEngine.Utf8;

    public static CsvEngine Select(TextWriter _) => CsvEngine.Utf16;
}
