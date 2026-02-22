using System;

namespace CsvForge.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class CsvColumnAttribute : Attribute
{
    public CsvColumnAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public int? Order { get; init; }
}
