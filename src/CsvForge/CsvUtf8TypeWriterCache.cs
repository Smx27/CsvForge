using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace CsvForge;

public static class CsvUtf8TypeWriterCache<T>
{
    private static ICsvUtf8TypeWriter<T>? _writer;
    private static int _resolved;

    public static ICsvUtf8TypeWriter<T>? Resolve()
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

        var contract = typeof(ICsvUtf8TypeWriter<T>);
        var implementation = typeof(T).Assembly
            .GetTypes()
            .FirstOrDefault(type => !type.IsAbstract && !type.IsInterface && contract.IsAssignableFrom(type));

        if (implementation is null)
        {
            return null;
        }

        var instanceField = implementation.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceField?.GetValue(null) is ICsvUtf8TypeWriter<T> instance)
        {
            Volatile.Write(ref _writer, instance);
            return instance;
        }

        if (Activator.CreateInstance(implementation) is ICsvUtf8TypeWriter<T> created)
        {
            Volatile.Write(ref _writer, created);
            return created;
        }

        return null;
    }
}
