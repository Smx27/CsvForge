using System;

namespace CsvForge.Attributes;

/// <summary>
/// Indicates that a property or field should be ignored during CSV serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class CsvIgnoreAttribute : Attribute
{
}
