using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace CsvForge;

public static class CsvTypeWriterCache<T>
{
    private static ICsvTypeWriter<T>? _writer;
    private static int _resolved;

    public static ICsvTypeWriter<T>? Resolve()
    {
        var existing = Volatile.Read(ref _writer);
        if (existing is not null)
        {
            return existing;
        }

        if (Interlocked.Exchange(ref _resolved, 1) != 0)
        {
            return null;
        }

        var contract = typeof(ICsvTypeWriter<T>);
        var implementation = typeof(T).Assembly
            .GetTypes()
            .FirstOrDefault(type => !type.IsAbstract && !type.IsInterface && contract.IsAssignableFrom(type));

        if (implementation is null)
        {
            return null;
        }

        var instanceField = implementation.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceField?.GetValue(null) is ICsvTypeWriter<T> instance)
        {
            Volatile.Write(ref _writer, instance);
            return instance;
        }

        if (Activator.CreateInstance(implementation) is ICsvTypeWriter<T> created)
        {
            Volatile.Write(ref _writer, created);
            return created;
        }

        return null;
    }

    public static void Register(ICsvTypeWriter<T> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Volatile.Write(ref _writer, writer);
        Volatile.Write(ref _resolved, 1);
    }
}
