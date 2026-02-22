using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using CsvForge.Attributes;

namespace CsvForge.Metadata;

internal static class TypeMetadataCache
{
    private static readonly ConcurrentDictionary<Type, object> TypedCache = new();

    public static TypeMetadata<T> GetOrAdd<T>()
    {
        return (TypeMetadata<T>)TypedCache.GetOrAdd(typeof(T), static _ => BuildTypeMetadata<T>());
    }

    private static TypeMetadata<T> BuildTypeMetadata<T>()
    {
        var columns = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.CanRead && property.GetMethod is not null)
            .Select(BuildColumnMetadata<T>)
            .OrderBy(static column => column.Order.HasValue ? 0 : 1)
            .ThenBy(static column => column.Order)
            .ThenBy(static column => column.DeclarationOrder)
            .ThenBy(static column => column.PropertyName, StringComparer.Ordinal)
            .ToArray();

        var typedColumns = new IColumnWriter<T>[columns.Length];
        for (var i = 0; i < columns.Length; i++)
        {
            typedColumns[i] = columns[i].CreateWriter();
        }

        return new TypeMetadata<T>(typedColumns);
    }

    private static ColumnDefinition<T> BuildColumnMetadata<T>(PropertyInfo property)
    {
        var csvAttribute = property.GetCustomAttribute<CsvColumnAttribute>();
        var jsonAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();

        var columnName = csvAttribute?.Name
            ?? jsonAttribute?.Name
            ?? property.Name;

        return new ColumnDefinition<T>(
            property.Name,
            columnName,
            csvAttribute?.Order,
            GetDeclarationOrder(property),
            property,
            BuildGetter<T>(property));
    }

    private static Func<T, TProperty> BuildGetter<T, TProperty>(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(T), "instance");
        var propertyAccess = Expression.Property(instance, property);
        return Expression.Lambda<Func<T, TProperty>>(propertyAccess, instance).Compile();
    }

    private static Delegate BuildGetter<T>(PropertyInfo property)
    {
        var buildMethod = typeof(TypeMetadataCache)
            .GetMethod(nameof(BuildGetter), BindingFlags.NonPublic | BindingFlags.Static, binder: null, new[] { typeof(PropertyInfo) }, modifiers: null)!;

        var genericMethod = buildMethod.MakeGenericMethod(typeof(T), property.PropertyType);
        return (Delegate)genericMethod.Invoke(null, new object[] { property })!;
    }

    private static int GetDeclarationOrder(MemberInfo property)
    {
        try
        {
            return property.MetadataToken;
        }
        catch
        {
            return int.MaxValue;
        }
    }
}

internal sealed record TypeMetadata<T>(IColumnWriter<T>[] Columns);

internal interface IColumnWriter<T>
{
    string ColumnName { get; }

    void Write(TextWriter writer, T item, CsvSerializationContext context);

    ValueTask WriteAsync(TextWriter writer, T item, CsvSerializationContext context, CancellationToken cancellationToken);
}

internal sealed record ColumnDefinition<T>(
    string PropertyName,
    string ColumnName,
    int? Order,
    int DeclarationOrder,
    PropertyInfo Property,
    Delegate Getter)
{
    public IColumnWriter<T> CreateWriter()
    {
        var columnWriterType = typeof(ColumnWriter<,>).MakeGenericType(typeof(T), Property.PropertyType);
        return (IColumnWriter<T>)Activator.CreateInstance(columnWriterType, ColumnName, Getter)!;
    }
}

internal sealed class ColumnWriter<T, TProperty> : IColumnWriter<T>
{
    private readonly Func<T, TProperty> _getter;

    public string ColumnName { get; }

    public ColumnWriter(string columnName, Delegate getter)
    {
        ColumnName = columnName;
        _getter = (Func<T, TProperty>)getter;
    }

    public void Write(TextWriter writer, T item, CsvSerializationContext context)
    {
        CsvValueFormatter.WriteField(writer, _getter(item), context);
    }

    public ValueTask WriteAsync(TextWriter writer, T item, CsvSerializationContext context, CancellationToken cancellationToken)
    {
        return CsvValueFormatter.WriteFieldAsync(writer, _getter(item), context, cancellationToken);
    }
}
