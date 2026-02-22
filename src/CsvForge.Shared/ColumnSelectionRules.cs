using System;

namespace CsvForge.Shared;

internal static class ColumnSelectionRules
{
    public static bool ShouldIncludeProperty(bool canRead, bool hasGetter, bool isPublicGetter, bool isStatic, bool isIndexer, bool hasCsvIgnoreAttribute)
    {
        if (!canRead || !hasGetter || isStatic || isIndexer || hasCsvIgnoreAttribute)
        {
            return false;
        }

        return isPublicGetter;
    }

    public static string ResolveColumnName(string? csvColumnName, string? jsonPropertyName, string propertyName)
    {
        return csvColumnName
            ?? jsonPropertyName
            ?? propertyName;
    }

    public static int Compare(ColumnOrderKey left, ColumnOrderKey right)
    {
        var leftPriority = left.Order.HasValue ? 0 : 1;
        var rightPriority = right.Order.HasValue ? 0 : 1;
        var byPriority = leftPriority.CompareTo(rightPriority);
        if (byPriority != 0)
        {
            return byPriority;
        }

        var byOrder = Nullable.Compare(left.Order, right.Order);
        if (byOrder != 0)
        {
            return byOrder;
        }

        var byDeclaration = left.DeclarationOrder.CompareTo(right.DeclarationOrder);
        if (byDeclaration != 0)
        {
            return byDeclaration;
        }

        return StringComparer.Ordinal.Compare(left.PropertyName, right.PropertyName);
    }
}

internal readonly struct ColumnOrderKey
{
    public ColumnOrderKey(int? order, int declarationOrder, string propertyName)
    {
        Order = order;
        DeclarationOrder = declarationOrder;
        PropertyName = propertyName;
    }

    public int? Order { get; }

    public int DeclarationOrder { get; }

    public string PropertyName { get; }
}
