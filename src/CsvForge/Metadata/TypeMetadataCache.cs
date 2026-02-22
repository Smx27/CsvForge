using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using CsvForge.Attributes;
using CsvForge.Shared;

namespace CsvForge.Metadata;

internal static class TypeMetadataCache
{
    private static readonly ConcurrentDictionary<Type, object> TypedCache = new();
    private static readonly ConcurrentDictionary<Type, RuntimeTypeMetadata> RuntimeCache = new();

    [RequiresUnreferencedCode("Runtime metadata fallback uses reflection over public properties and is not trimming-safe. Prefer generated ICsvTypeWriter<T> implementations.")]
    public static TypeMetadata<T> GetOrAdd<T>()
    {
        return (TypeMetadata<T>)TypedCache.GetOrAdd(typeof(T), static _ => BuildTypeMetadata<T>());
    }

    public static RuntimeTypeMetadata GetOrAddRuntime(Type type)
    {
        return RuntimeCache.GetOrAdd(type, BuildRuntimeTypeMetadata);
    }

    [RequiresUnreferencedCode("Runtime metadata fallback uses reflection over public properties and is not trimming-safe. Prefer generated ICsvTypeWriter<T> implementations.")]
    private static TypeMetadata<T> BuildTypeMetadata<T>()
    {
        var columns = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => ColumnSelectionRules.ShouldIncludeProperty(
                property.CanRead,
                property.GetMethod is not null,
                property.GetMethod?.IsPublic == true,
                property.GetMethod?.IsStatic == true,
                property.GetIndexParameters().Length > 0,
                property.GetCustomAttribute<CsvIgnoreAttribute>() is not null))
            .Select(BuildColumnMetadata<T>)
            .ToArray();

        Array.Sort(columns, static (left, right) => ColumnSelectionRules.Compare(left.SortKey, right.SortKey));

        var typedColumns = new IColumnWriter<T>[columns.Length];
        for (var i = 0; i < columns.Length; i++)
        {
            typedColumns[i] = columns[i].CreateWriter();
        }

        return new TypeMetadata<T>(typedColumns);
    }

    private static RuntimeTypeMetadata BuildRuntimeTypeMetadata(Type type)
    {
        var columns = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => ColumnSelectionRules.ShouldIncludeProperty(
                property.CanRead,
                property.GetMethod is not null,
                property.GetMethod?.IsPublic == true,
                property.GetMethod?.IsStatic == true,
                property.GetIndexParameters().Length > 0,
                property.GetCustomAttribute<CsvIgnoreAttribute>() is not null))
            .Select(BuildRuntimeColumnMetadata)
            .ToArray();

        Array.Sort(columns, static (left, right) => ColumnSelectionRules.Compare(left.SortKey, right.SortKey));

        var columnLookup = new Dictionary<string, Func<object, object?>>(columns.Length, StringComparer.Ordinal);
        for (var i = 0; i < columns.Length; i++)
        {
            columnLookup[columns[i].ColumnName] = columns[i].Getter;
        }

        return new RuntimeTypeMetadata(columns, columnLookup);
    }

    private static ColumnDefinition<T> BuildColumnMetadata<T>(PropertyInfo property)
    {
        var details = GetColumnDetails(property);

        return new ColumnDefinition<T>(
            details.PropertyName,
            details.ColumnName,
            details.Order,
            details.DeclarationOrder,
            property);
    }

    private static RuntimeColumnDefinition BuildRuntimeColumnMetadata(PropertyInfo property)
    {
        var details = GetColumnDetails(property);
        return new RuntimeColumnDefinition(details.PropertyName, details.ColumnName, details.Order, details.DeclarationOrder, BuildRuntimeGetter(property));
    }

    private static ColumnDetails GetColumnDetails(PropertyInfo property)
    {
        var csvAttribute = property.GetCustomAttribute<CsvColumnAttribute>();
        var jsonAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();

        var columnName = ColumnSelectionRules.ResolveColumnName(csvAttribute?.Name, jsonAttribute?.Name, property.Name);

        return new ColumnDetails(
            property.Name,
            columnName,
            csvAttribute?.Order,
            GetDeclarationOrder(property));
    }

    private static Func<object, object?> BuildRuntimeGetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var cast = Expression.Convert(instance, property.DeclaringType!);
        var propertyAccess = Expression.Property(cast, property);
        var box = Expression.Convert(propertyAccess, typeof(object));
        return Expression.Lambda<Func<object, object?>>(box, instance).Compile();
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

internal sealed record RuntimeTypeMetadata(
    RuntimeColumnDefinition[] Columns,
    IReadOnlyDictionary<string, Func<object, object?>> ColumnLookup);

internal sealed record RuntimeColumnDefinition(
    string PropertyName,
    string ColumnName,
    int? Order,
    int DeclarationOrder,
    Func<object, object?> Getter)
{
    public ColumnOrderKey SortKey => new(Order, DeclarationOrder, PropertyName);
}

internal sealed record ColumnDetails(string PropertyName, string ColumnName, int? Order, int DeclarationOrder);

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
    PropertyInfo Property)
{
    public ColumnOrderKey SortKey => new(Order, DeclarationOrder, PropertyName);

    public IColumnWriter<T> CreateWriter()
    {
        return new ReflectiveColumnWriter<T>(ColumnName, Property);
    }
}

internal sealed class ReflectiveColumnWriter<T> : IColumnWriter<T>
{
    private readonly PropertyInfo _property;

    public string ColumnName { get; }

    public ReflectiveColumnWriter(string columnName, PropertyInfo property)
    {
        ColumnName = columnName;
        _property = property;
    }

    public void Write(TextWriter writer, T item, CsvSerializationContext context)
    {
        CsvValueFormatter.WriteField(writer, _property.GetValue(item), context);
    }

    public ValueTask WriteAsync(TextWriter writer, T item, CsvSerializationContext context, CancellationToken cancellationToken)
    {
        return CsvValueFormatter.WriteFieldAsync(writer, _property.GetValue(item), context, cancellationToken);
    }
}
