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

public enum CsvHeterogeneousHeaderBehavior
{
    /// <summary>
    /// Builds headers from the union of all encountered row shapes.
    /// </summary>
    Union,

    /// <summary>
    /// Locks headers to the first encountered non-empty row shape.
    /// Additional fields from subsequent rows are ignored.
    /// </summary>
    FirstShapeLock
}

public enum CsvUnionAsyncBehavior
{
    /// <summary>
    /// Throws when union header discovery requires replaying a non-replayable source.
    /// </summary>
    Throw,

    /// <summary>
    /// Falls back to first-shape-lock semantics for non-replayable async sources.
    /// </summary>
    FirstShapeLock
}

public enum CsvCompressionMode
{
    None,
    Gzip,
    Zip
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

    /// <summary>
    /// Controls header generation when serializing heterogeneous rows (for example, <c>IEnumerable&lt;object&gt;</c> or dictionary-based rows).
    /// </summary>
    public CsvHeterogeneousHeaderBehavior HeterogeneousHeaderBehavior { get; init; } = CsvHeterogeneousHeaderBehavior.Union;

    /// <summary>
    /// Controls behavior for union header discovery when serializing <see cref="IAsyncEnumerable{T}"/> dynamic rows.
    /// </summary>
    public CsvUnionAsyncBehavior UnionAsyncBehavior { get; init; } = CsvUnionAsyncBehavior.Throw;

    /// <summary>
    /// Enables runtime reflection-based metadata fallback when no generated <c>ICsvTypeWriter&lt;T&gt;</c> is available.
    /// </summary>
    /// <remarks>
    /// This fallback is intended for non-AOT scenarios. Keep this option disabled in NativeAOT or trimmed applications and prefer generated writers.
    /// </remarks>
    public bool EnableRuntimeMetadataFallback { get; init; }

    /// <summary>
    /// Controls whether CSV output is written as plain text, GZip, or a single-entry ZIP archive.
    /// </summary>
    public CsvCompressionMode Compression { get; init; } = CsvCompressionMode.None;

    internal string NewLine => NewLineBehavior switch
    {
        CsvNewLineBehavior.Lf => "\n",
        CsvNewLineBehavior.CrLf => "\r\n",
        _ => Environment.NewLine
    };
}
