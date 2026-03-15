using System;

namespace CsvForge.Attributes;

/// <summary>
/// Indicates that a class or struct is serializable to CSV and should have a writer generated for it.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class CsvSerializableAttribute : Attribute
{
}
