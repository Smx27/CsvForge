using System;

namespace CsvForge.Attributes;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class CsvIgnoreAttribute : Attribute
{
}
