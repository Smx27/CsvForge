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
    /// Overrides line endings used for row termination. When set, this takes precedence over <see cref="NewLineBehavior"/>.
    /// </summary>
    public string? ExplicitNewLine { get; init; }

    /// <summary>
    /// Enables Excel-oriented defaults such as CRLF line endings and UTF-8 BOM emission for stream/file targets.
    /// </summary>
    public bool ExcelCompatibility { get; init; }

    /// <summary>
    /// Emits a UTF-8 BOM when writing to stream/file targets.
    /// </summary>
    public bool EmitUtf8Bom { get; init; }

    /// <summary>
    /// Enables delimiter fallback for decimal-comma cultures when using Excel compatibility mode.
    /// </summary>
    public bool EnableExcelDelimiterFallbackByCulture { get; init; } = true;

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

    internal string NewLine => ExplicitNewLine ?? (NewLineBehavior switch
    {
        CsvNewLineBehavior.Lf => "\n",
        CsvNewLineBehavior.CrLf => "\r\n",
        _ => Environment.NewLine
    });

    internal bool UseExcelEscaping => ExcelCompatibility;

    internal static CsvOptions NormalizeForWrite(CsvOptions options, bool streamOrFileTarget)
    {
        var normalized = options;

        if (options.ExcelCompatibility)
        {
            if (options.ExplicitNewLine is null && options.NewLineBehavior == CsvNewLineBehavior.Environment)
            {
                normalized = normalized with { ExplicitNewLine = "\r\n" };
            }

            if (options.EnableExcelDelimiterFallbackByCulture && options.Delimiter == ',')
            {
                var culture = options.FormatProvider as CultureInfo ?? CultureInfo.CurrentCulture;
                if (string.Equals(culture.NumberFormat.NumberDecimalSeparator, ",", StringComparison.Ordinal))
                {
                    normalized = normalized with { Delimiter = ';' };
                }
            }

            if (streamOrFileTarget && IsUtf8Encoding(options.Encoding))
            {
                normalized = normalized with { EmitUtf8Bom = true };
            }
        }

        return normalized;
    }

    private static bool IsUtf8Encoding(Encoding encoding) => encoding.CodePage == Encoding.UTF8.CodePage;
}
