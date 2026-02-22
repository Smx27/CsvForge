using System;

namespace CsvForge.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class CsvSerializableAttribute : Attribute
{
}
