using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using CsvForge.Attributes;

namespace CsvForge.Metadata;

internal static class TypeMetadataCache
{
    private static readonly ConcurrentDictionary<Type, TypeMetadata> Cache = new();

    public static TypeMetadata GetOrAdd(Type type)
    {
        return Cache.GetOrAdd(type, BuildTypeMetadata);
    }

    private static TypeMetadata BuildTypeMetadata(Type type)
    {
        var columns = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(static property => property.CanRead && property.GetMethod is not null)
            .Select(BuildColumnMetadata)
            .OrderBy(static column => column.Order.HasValue ? 0 : 1)
            .ThenBy(static column => column.Order)
            .ThenBy(static column => column.DeclarationOrder)
            .ThenBy(static column => column.PropertyName, StringComparer.Ordinal)
            .ToArray();

        return new TypeMetadata(type, columns);
    }

    private static ColumnMetadata BuildColumnMetadata(PropertyInfo property)
    {
        var csvAttribute = property.GetCustomAttribute<CsvColumnAttribute>();
        var jsonAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();

        var columnName = csvAttribute?.Name
            ?? jsonAttribute?.Name
            ?? property.Name;

        return new ColumnMetadata(
            property.Name,
            columnName,
            csvAttribute?.Order,
            GetDeclarationOrder(property),
            BuildGetter(property));
    }

    private static Func<object, object?> BuildGetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var typedInstance = Expression.Convert(instance, property.DeclaringType!);
        var propertyAccess = Expression.Property(typedInstance, property);
        var castResult = Expression.Convert(propertyAccess, typeof(object));

        return Expression.Lambda<Func<object, object?>>(castResult, instance).Compile();
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

internal sealed record TypeMetadata(Type Type, ColumnMetadata[] Columns);

internal sealed record ColumnMetadata(
    string PropertyName,
    string ColumnName,
    int? Order,
    int DeclarationOrder,
    Func<object, object?> Getter);
