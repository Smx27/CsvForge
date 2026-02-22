using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CsvForge;

internal static class CsvSerializer
{
    private static readonly ConcurrentDictionary<Type, object> AccessorCache = new();

    public static void Write<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        var accessors = GetAccessors<T>();

        if (options.IncludeHeader)
        {
            WriteHeader(writer, accessors, options);
        }

        foreach (var item in data)
        {
            WriteRecord(writer, item, accessors, options);
        }
    }

    public static async Task WriteAsync<T>(IEnumerable<T> data, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        var accessors = GetAccessors<T>();

        if (options.IncludeHeader)
        {
            await WriteHeaderAsync(writer, accessors, options, cancellationToken).ConfigureAwait(false);
        }

        foreach (var item in data)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await WriteRecordAsync(writer, item, accessors, options, cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task WriteAsync<T>(IAsyncEnumerable<T> data, TextWriter writer, CsvOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(writer);

        var accessors = GetAccessors<T>();

        if (options.IncludeHeader)
        {
            await WriteHeaderAsync(writer, accessors, options, cancellationToken).ConfigureAwait(false);
        }

        await foreach (var item in data.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            await WriteRecordAsync(writer, item, accessors, options, cancellationToken).ConfigureAwait(false);
        }
    }

    private static Accessor<T>[] GetAccessors<T>()
    {
        return (Accessor<T>[])AccessorCache.GetOrAdd(typeof(T), static _ => CreateAccessors<T>());
    }

    private static Accessor<T>[] CreateAccessors<T>()
    {
        var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var accessors = new Accessor<T>[properties.Length];

        for (var i = 0; i < properties.Length; i++)
        {
            var property = properties[i];
            var parameter = Expression.Parameter(typeof(T), "instance");
            Expression body = Expression.Property(parameter, property);
            if (property.PropertyType.IsValueType)
            {
                body = Expression.Convert(body, typeof(object));
            }

            var getter = Expression.Lambda<Func<T, object?>>(body, parameter).Compile();
            accessors[i] = new Accessor<T>(property.Name, getter);
        }

        return accessors;
    }

    private static void WriteHeader<T>(TextWriter writer, Accessor<T>[] accessors, CsvOptions options)
    {
        for (var i = 0; i < accessors.Length; i++)
        {
            if (i > 0)
            {
                writer.Write(options.Delimiter);
            }

            WriteField(writer, accessors[i].Name, options);
        }

        writer.Write(options.NewLine);
    }

    private static async Task WriteHeaderAsync<T>(TextWriter writer, Accessor<T>[] accessors, CsvOptions options, CancellationToken cancellationToken)
    {
        for (var i = 0; i < accessors.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (i > 0)
            {
                await writer.WriteAsync(options.Delimiter).ConfigureAwait(false);
            }

            await WriteFieldAsync(writer, accessors[i].Name, options, cancellationToken).ConfigureAwait(false);
        }

        await writer.WriteAsync(options.NewLine).ConfigureAwait(false);
    }

    private static void WriteRecord<T>(TextWriter writer, T item, Accessor<T>[] accessors, CsvOptions options)
    {
        for (var i = 0; i < accessors.Length; i++)
        {
            if (i > 0)
            {
                writer.Write(options.Delimiter);
            }

            var value = accessors[i].Getter(item);
            WriteField(writer, FormatValue(value, options.FormatProvider), options);
        }

        writer.Write(options.NewLine);
    }

    private static async Task WriteRecordAsync<T>(TextWriter writer, T item, Accessor<T>[] accessors, CsvOptions options, CancellationToken cancellationToken)
    {
        for (var i = 0; i < accessors.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (i > 0)
            {
                await writer.WriteAsync(options.Delimiter).ConfigureAwait(false);
            }

            var value = accessors[i].Getter(item);
            await WriteFieldAsync(writer, FormatValue(value, options.FormatProvider), options, cancellationToken).ConfigureAwait(false);
        }

        await writer.WriteAsync(options.NewLine).ConfigureAwait(false);
    }

    private static string? FormatValue(object? value, IFormatProvider formatProvider)
    {
        if (value is null)
        {
            return null;
        }

        if (value is IFormattable formattable)
        {
            return formattable.ToString(null, formatProvider);
        }

        return Convert.ToString(value, formatProvider);
    }

    private static void WriteField(TextWriter writer, string? value, CsvOptions options)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (!NeedsEscaping(value, options.Delimiter, options.NewLine))
        {
            writer.Write(value);
            return;
        }

        writer.Write('"');
        var start = 0;
        while (true)
        {
            var quoteIndex = value.IndexOf('"', start);
            if (quoteIndex < 0)
            {
                writer.Write(value.AsSpan(start));
                break;
            }

            writer.Write(value.AsSpan(start, quoteIndex - start));
            writer.Write("\"\"");
            start = quoteIndex + 1;
        }

        writer.Write('"');
    }

    private static async Task WriteFieldAsync(TextWriter writer, string? value, CsvOptions options, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (!NeedsEscaping(value, options.Delimiter, options.NewLine))
        {
            await writer.WriteAsync(value).ConfigureAwait(false);
            return;
        }

        await writer.WriteAsync("\"").ConfigureAwait(false);
        var start = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var quoteIndex = value.IndexOf('"', start);
            if (quoteIndex < 0)
            {
                await writer.WriteAsync(value.AsMemory(start)).ConfigureAwait(false);
                break;
            }

            await writer.WriteAsync(value.AsMemory(start, quoteIndex - start)).ConfigureAwait(false);
            await writer.WriteAsync("\"\"").ConfigureAwait(false);
            start = quoteIndex + 1;
        }

        await writer.WriteAsync("\"").ConfigureAwait(false);
    }

    private static bool NeedsEscaping(string value, char delimiter, string newLine)
    {
        if (value.IndexOfAny(SearchValues.Create([delimiter, '"', '\r', '\n'])) >= 0)
        {
            return true;
        }

        return !string.IsNullOrEmpty(newLine) && value.Contains(newLine, StringComparison.Ordinal);
    }

    private readonly record struct Accessor<T>(string Name, Func<T, object?> Getter);
}
