using System;

namespace CsvForge.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class CsvIgnoreAttribute : Attribute
{
}
