using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;
using CsvForge.Attributes;

namespace CsvForge.Metadata;

internal static class TypeMetadataCache
{
    private static readonly ConcurrentDictionary<Type, TypeMetadata> Cache = new();
    private static readonly ConcurrentDictionary<Type, Func<object?, IFormatProvider, string?>> FormatterCache = new();

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
            BuildGetter(property),
            GetOrCreateFormatter(property.PropertyType));
    }

    private static Func<object, object?> BuildGetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var typedInstance = Expression.Convert(instance, property.DeclaringType!);
        var propertyAccess = Expression.Property(typedInstance, property);
        var castResult = Expression.Convert(propertyAccess, typeof(object));

        return Expression.Lambda<Func<object, object?>>(castResult, instance).Compile();
    }

    private static Func<object?, IFormatProvider, string?> GetOrCreateFormatter(Type propertyType)
    {
        return FormatterCache.GetOrAdd(propertyType, CreateFormatter);
    }

    private static Func<object?, IFormatProvider, string?> CreateFormatter(Type propertyType)
    {
        var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        var converter = TypeDescriptor.GetConverter(targetType);
        var cultureAwareConverter = converter.CanConvertTo(typeof(string));

        return (value, provider) =>
        {
            if (value is null)
            {
                return null;
            }

            if (value is IFormattable formattable)
            {
                return formattable.ToString(null, provider);
            }

            if (cultureAwareConverter)
            {
                return converter.ConvertToString(null, provider as CultureInfo ?? CultureInfo.CurrentCulture, value);
            }

            return Convert.ToString(value, provider);
        };
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
    Func<object, object?> Getter,
    Func<object?, IFormatProvider, string?> Formatter);
