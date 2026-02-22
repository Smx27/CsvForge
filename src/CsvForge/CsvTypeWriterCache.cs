using System;
using System.Threading;

namespace CsvForge;

public static class CsvTypeWriterCache<T>
{
    private static ICsvTypeWriter<T>? _writer;

    public static ICsvTypeWriter<T>? Resolve()
    {
        var registered = Volatile.Read(ref _writer);
        return registered ?? GeneratedRegistration<T>.Writer;
    }

    public static void Register(ICsvTypeWriter<T> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Volatile.Write(ref _writer, writer);
    }

    internal static void RegisterGenerated(ICsvTypeWriter<T> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        GeneratedRegistration<T>.Writer = writer;
    }

    private static class GeneratedRegistration<TType>
    {
        public static ICsvTypeWriter<TType>? Writer;
    }
}
