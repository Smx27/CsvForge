using System;
using System.Threading;

namespace CsvForge;

/// <summary>
/// A cache for resolution of <see cref="ICsvTypeWriter{T}"/>.
/// </summary>
/// <typeparam name="T">The type of the data row.</typeparam>
public static class CsvTypeWriterCache<T>
{
    private static ICsvTypeWriter<T>? _writer;

    /// <summary>
    /// Resolves the available writer for the type.
    /// </summary>
    /// <returns>The resolved writer or null.</returns>
    public static ICsvTypeWriter<T>? Resolve()
    {
        var registered = Volatile.Read(ref _writer);
        return registered ?? GeneratedRegistration<T>.Writer;
    }

    /// <summary>
    /// Registers a custom writer for the type.
    /// </summary>
    /// <param name="writer">The writer to register.</param>
    public static void Register(ICsvTypeWriter<T> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Volatile.Write(ref _writer, writer);
    }

    /// <summary>
    /// Registers a source-generated writer for the type.
    /// </summary>
    /// <param name="writer">The generated writer to register.</param>
    public static void RegisterGenerated(ICsvTypeWriter<T> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        GeneratedRegistration<T>.Writer = writer;
    }

    private static class GeneratedRegistration<TType>
    {
        public static ICsvTypeWriter<TType>? Writer;
    }
}
