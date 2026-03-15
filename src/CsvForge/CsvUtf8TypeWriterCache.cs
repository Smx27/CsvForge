using System;
using System.Threading;

namespace CsvForge;

/// <summary>
/// A cache for resolution of <see cref="ICsvUtf8TypeWriter{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the data row.</typeparam>
public static class CsvUtf8TypeWriterCache<T>
{
    private static ICsvUtf8TypeWriter<T>? _writer;

    /// <summary>
    /// Resolves the available writer for the type.
    /// </summary>
    /// <returns>The resolved writer or null.</returns>
    public static ICsvUtf8TypeWriter<T>? Resolve()
    {
        var registered = Volatile.Read(ref _writer);
        return registered ?? GeneratedRegistration<T>.Writer;
    }

    /// <summary>
    /// Registers a custom writer for the type.
    /// </summary>
    /// <param name="writer">The writer to register.</param>
    public static void Register(ICsvUtf8TypeWriter<T> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Volatile.Write(ref _writer, writer);
    }

    /// <summary>
    /// Registers a source-generated writer for the type.
    /// </summary>
    /// <param name="writer">The generated writer to register.</param>
    public static void RegisterGenerated(ICsvUtf8TypeWriter<T> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        GeneratedRegistration<T>.Writer = writer;
    }

    private static class GeneratedRegistration<TType>
    {
        public static ICsvUtf8TypeWriter<TType>? Writer;
    }
}
