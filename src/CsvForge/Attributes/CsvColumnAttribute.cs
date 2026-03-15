using System;

namespace CsvForge.Attributes;

/// <summary>
/// Specifies the name and order of a CSV column for a property.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class CsvColumnAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvColumnAttribute"/> class.
    /// </summary>
    /// <param name="name">The name of the CSV column.</param>
    public CsvColumnAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the name of the CSV column.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the order of the CSV column.
    /// </summary>
    public int Order { get; init; } = int.MaxValue;
}
