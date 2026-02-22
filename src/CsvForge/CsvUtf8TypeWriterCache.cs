using System;
using System.Threading;

namespace CsvForge;

public static class CsvUtf8TypeWriterCache<T>
{
    private static ICsvUtf8TypeWriter<T>? _writer;

    public static ICsvUtf8TypeWriter<T>? Resolve()
    {
        var registered = Volatile.Read(ref _writer);
        return registered ?? GeneratedRegistration<T>.Writer;
    }

    public static void Register(ICsvUtf8TypeWriter<T> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Volatile.Write(ref _writer, writer);
    }

    internal static void RegisterGenerated(ICsvUtf8TypeWriter<T> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        GeneratedRegistration<T>.Writer = writer;
    }

    private static class GeneratedRegistration<TType>
    {
        public static ICsvUtf8TypeWriter<TType>? Writer;
    }
}
