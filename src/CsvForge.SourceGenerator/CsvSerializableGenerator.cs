using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using CsvForge.Shared;

namespace CsvForge.SourceGenerator;

[Generator]
public sealed class CsvSerializableGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor UnsupportedProperty = new(
        "CSVGEN001",
        "Unsupported CSV member",
        "Member '{0}' on type '{1}' is unsupported for CSV source generation",
        "CsvForge.Generator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor GenericTypeUnsupported = new(
        "CSVGEN002",
        "Generic type not supported",
        "Type '{0}' is generic and cannot be used with CsvSerializable source generation",
        "CsvForge.Generator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax t && t.AttributeLists.Count > 0,
                static (ctx, _) => GetCandidate(ctx))
            .Where(static c => c is not null)
            .Select(static (c, _) => c!);

        var compilationAndCandidates = context.CompilationProvider.Combine(candidates.Collect());
        context.RegisterSourceOutput(compilationAndCandidates, static (spc, source) => Execute(spc, source.Right));
    }

    private static INamedTypeSymbol? GetCandidate(GeneratorSyntaxContext context)
    {
        if (context.Node is not TypeDeclarationSyntax typeDeclaration)
        {
            return null;
        }

        var symbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
        if (symbol is null)
        {
            return null;
        }

        foreach (var attribute in symbol.GetAttributes())
        {
            var attributeClass = attribute.AttributeClass;
            if (attributeClass?.ToDisplayString() == "CsvForge.Attributes.CsvSerializableAttribute")
            {
                return symbol;
            }
        }

        return null;
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<INamedTypeSymbol> candidates)
    {
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var symbol in candidates)
        {
            if (!seen.Add(symbol))
            {
                continue;
            }

            if (symbol.TypeParameters.Length > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(GenericTypeUnsupported, symbol.Locations.FirstOrDefault(), symbol.ToDisplayString()));
                continue;
            }

            var columns = CollectColumns(context, symbol);
            EmitWriters(context, symbol, columns);
        }
    }

    private static List<ColumnModel> CollectColumns(SourceProductionContext context, INamedTypeSymbol symbol)
    {
        var result = new List<ColumnModel>();
        foreach (var member in symbol.GetMembers().OrderBy(static m => m.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue).ThenBy(static m => m.Name, StringComparer.Ordinal))
        {
            if (member is not IPropertySymbol property)
            {
                continue;
            }

            var hasIgnore = HasCsvIgnore(property);
            var include = ColumnSelectionRules.ShouldIncludeProperty(
                canRead: true,
                hasGetter: property.GetMethod is not null,
                isPublicGetter: property.GetMethod?.DeclaredAccessibility == Accessibility.Public,
                isStatic: property.IsStatic,
                isIndexer: property.Parameters.Length > 0,
                hasCsvIgnoreAttribute: hasIgnore);

            if (!include)
            {
                if (!hasIgnore)
                {
                    context.ReportDiagnostic(Diagnostic.Create(UnsupportedProperty, property.Locations.FirstOrDefault(), property.Name, symbol.ToDisplayString()));
                }

                continue;
            }

            var order = GetOrder(property);
            var name = GetName(property);
            var isNullable = property.NullableAnnotation == NullableAnnotation.Annotated || property.Type.IsReferenceType;
            result.Add(new ColumnModel(property.Name, name, order, property.Type, isNullable, property.Locations.FirstOrDefault()?.SourceSpan.Start ?? int.MaxValue));
        }

        result.Sort(static (left, right) => ColumnSelectionRules.Compare(left.SortKey, right.SortKey));
        return result;
    }

    private static int? GetOrder(IPropertySymbol property)
    {
        foreach (var attr in property.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != "CsvForge.Attributes.CsvColumnAttribute")
            {
                continue;
            }

            foreach (var named in attr.NamedArguments)
            {
                if (named.Key == "Order" && named.Value.Value is int value)
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static string GetName(IPropertySymbol property)
    {
        string? csvColumnName = null;
        string? jsonPropertyName = null;

        foreach (var attr in property.GetAttributes())
        {
            var typeName = attr.AttributeClass?.ToDisplayString();
            if (typeName == "CsvForge.Attributes.CsvColumnAttribute" && attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Value is string csvName)
            {
                csvColumnName = csvName;
                continue;
            }

            if (typeName == "System.Text.Json.Serialization.JsonPropertyNameAttribute" && attr.ConstructorArguments.Length == 1 && attr.ConstructorArguments[0].Value is string jsonName)
            {
                jsonPropertyName = jsonName;
            }
        }

        return ColumnSelectionRules.ResolveColumnName(csvColumnName, jsonPropertyName, property.Name);
    }

    private static bool HasCsvIgnore(IPropertySymbol property)
    {
        foreach (var attr in property.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == "CsvForge.Attributes.CsvIgnoreAttribute")
            {
                return true;
            }
        }

        return false;
    }

    private static void EmitWriters(SourceProductionContext context, INamedTypeSymbol symbol, List<ColumnModel> columns)
    {
        var ns = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();
        var targetType = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var writerBaseName = GetWriterBaseName(symbol);

        context.AddSource($"{writerBaseName}_CsvUtf16Writer.g.cs", SourceText.From(BuildUtf16Writer(ns, targetType, writerBaseName, columns), Encoding.UTF8));
        context.AddSource($"{writerBaseName}_CsvUtf8Writer.g.cs", SourceText.From(BuildUtf8Writer(ns, targetType, writerBaseName, columns), Encoding.UTF8));
    }

    private static string BuildUtf16Writer(string? ns, string targetType, string writerBaseName, List<ColumnModel> columns)
    {
        var writerTypeName = writerBaseName + "_CsvUtf16Writer";
        var source = new StringBuilder();
        source.AppendLine("using System;");
        source.AppendLine("using System.Globalization;");
        source.AppendLine("using System.IO;");
        source.AppendLine("using System.Threading;");
        source.AppendLine("using System.Threading.Tasks;");

        if (ns is not null)
        {
            source.Append("namespace ").Append(ns).AppendLine(";");
            source.AppendLine();
        }

        source.Append("file sealed class ").Append(writerTypeName).Append(" : global::CsvForge.ICsvTypeWriter<").Append(targetType).AppendLine(">")
            .AppendLine("{")
            .Append("    public static readonly ").Append(writerTypeName).AppendLine(" Instance = new();")
            .AppendLine("    private static readonly string[] HeaderColumns = new[]")
            .AppendLine("    {");

        foreach (var column in columns)
        {
            source.Append("        \"").Append(Escape(column.ColumnName)).AppendLine("\",");
        }

        source.AppendLine("    };")
            .AppendLine()
            .AppendLine("    public void WriteHeader(TextWriter writer, global::CsvForge.CsvOptions options)")
            .AppendLine("    {")
            .AppendLine("        for (var i = 0; i < HeaderColumns.Length; i++)")
            .AppendLine("        {")
            .AppendLine("            if (i > 0)")
            .AppendLine("            {")
            .AppendLine("                writer.Write(options.Delimiter);")
            .AppendLine("            }")
            .AppendLine("            global::CsvForge.CsvGeneratedWriterSupport.WriteEscaped(writer, HeaderColumns[i], options.Delimiter);")
            .AppendLine("        }")
            .AppendLine("    }")
            .AppendLine()
            .Append("    public void WriteRow(TextWriter writer, ").Append(targetType).AppendLine(" value, global::CsvForge.CsvOptions options)")
            .AppendLine("    {");

        AppendRowWrites(source, columns, utf8: false, isAsync: false);

        source.AppendLine("    }")
            .AppendLine()
            .AppendLine("    public async ValueTask WriteHeaderAsync(TextWriter writer, global::CsvForge.CsvOptions options, CancellationToken cancellationToken)")
            .AppendLine("    {")
            .AppendLine("        for (var i = 0; i < HeaderColumns.Length; i++)")
            .AppendLine("        {")
            .AppendLine("            if (i > 0)")
            .AppendLine("            {")
            .AppendLine("                await writer.WriteAsync(options.Delimiter).ConfigureAwait(false);")
            .AppendLine("            }")
            .AppendLine("            await global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedAsync(writer, HeaderColumns[i], options.Delimiter, cancellationToken).ConfigureAwait(false);")
            .AppendLine("        }")
            .AppendLine("    }")
            .AppendLine()
            .Append("    public async ValueTask WriteRowAsync(TextWriter writer, ").Append(targetType).AppendLine(" value, global::CsvForge.CsvOptions options, CancellationToken cancellationToken)")
            .AppendLine("    {");

        AppendRowWrites(source, columns, utf8: false, isAsync: true);

        source.AppendLine("    }")
            .AppendLine("}");
        return source.ToString();
    }

    private static string BuildUtf8Writer(string? ns, string targetType, string writerBaseName, List<ColumnModel> columns)
    {
        var writerTypeName = writerBaseName + "_CsvUtf8Writer";
        var source = new StringBuilder();
        source.AppendLine("using System;");
        source.AppendLine("using System.Buffers;");
        source.AppendLine("using System.Globalization;");
        source.AppendLine("using System.Text;");
        source.AppendLine("using System.Threading;");
        source.AppendLine("using System.Threading.Tasks;");

        if (ns is not null)
        {
            source.Append("namespace ").Append(ns).AppendLine(";");
            source.AppendLine();
        }

        source.Append("file sealed class ").Append(writerTypeName).Append(" : global::CsvForge.ICsvUtf8TypeWriter<").Append(targetType).AppendLine(">")
            .AppendLine("{")
            .Append("    public static readonly ").Append(writerTypeName).AppendLine(" Instance = new();")
            .AppendLine("    private static readonly byte[][] HeaderColumnsUtf8 = new[]")
            .AppendLine("    {");

        foreach (var column in columns)
        {
            source.Append("        Encoding.UTF8.GetBytes(\"").Append(Escape(column.ColumnName)).AppendLine("\"),");
        }

        source.AppendLine("    };")
            .AppendLine()
            .AppendLine("    public void WriteHeader(IBufferWriter<byte> writer, global::CsvForge.CsvOptions options)")
            .AppendLine("    {")
            .AppendLine("        for (var i = 0; i < HeaderColumnsUtf8.Length; i++)")
            .AppendLine("        {")
            .AppendLine("            if (i > 0)")
            .AppendLine("            {")
            .AppendLine("                var delimiter = writer.GetSpan(1);")
            .AppendLine("                delimiter[0] = (byte)options.Delimiter;")
            .AppendLine("                writer.Advance(1);")
            .AppendLine("            }")
            .AppendLine("            global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, HeaderColumnsUtf8[i], (byte)options.Delimiter);")
            .AppendLine("        }")
            .AppendLine("    }")
            .AppendLine()
            .Append("    public void WriteRow(IBufferWriter<byte> writer, ").Append(targetType).AppendLine(" value, global::CsvForge.CsvOptions options)")
            .AppendLine("    {");

        AppendRowWrites(source, columns, utf8: true, isAsync: false);

        source.AppendLine("    }")
            .AppendLine()
            .AppendLine("    public ValueTask WriteHeaderAsync(IBufferWriter<byte> writer, global::CsvForge.CsvOptions options, CancellationToken cancellationToken)")
            .AppendLine("    {")
            .AppendLine("        WriteHeader(writer, options);")
            .AppendLine("        return ValueTask.CompletedTask;")
            .AppendLine("    }")
            .AppendLine()
            .Append("    public ValueTask WriteRowAsync(IBufferWriter<byte> writer, ").Append(targetType).AppendLine(" value, global::CsvForge.CsvOptions options, CancellationToken cancellationToken)")
            .AppendLine("    {")
            .AppendLine("        WriteRow(writer, value, options);")
            .AppendLine("        return ValueTask.CompletedTask;")
            .AppendLine("    }")
            .AppendLine("}");

        return source.ToString();
    }

    private static void AppendRowWrites(StringBuilder source, List<ColumnModel> columns, bool utf8, bool isAsync)
    {
        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
            {
                if (isAsync)
                {
                    source.AppendLine("        cancellationToken.ThrowIfCancellationRequested();");
                }

                if (utf8)
                {
                    source.AppendLine("        var delimiter = writer.GetSpan(1);");
                    source.AppendLine("        delimiter[0] = (byte)options.Delimiter;");
                    source.AppendLine("        writer.Advance(1);");
                }
                else if (isAsync)
                {
                    source.AppendLine("        await writer.WriteAsync(options.Delimiter).ConfigureAwait(false);");
                }
                else
                {
                    source.AppendLine("        writer.Write(options.Delimiter);");
                }
            }

            AppendValueWrite(source, columns[i], utf8, isAsync);
        }
    }

    private static void AppendValueWrite(StringBuilder source, ColumnModel column, bool utf8, bool isAsync)
    {
        var accessor = "value." + column.PropertyName;
        if (column.IsNullable)
        {
            if (utf8)
            {
                source.Append("        global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, ").Append(accessor).AppendLine("?.ToString(), (byte)options.Delimiter);");
            }
            else if (isAsync)
            {
                source.Append("        await global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedAsync(writer, ").Append(accessor).AppendLine("?.ToString(), options.Delimiter, cancellationToken).ConfigureAwait(false);");
            }
            else
            {
                source.Append("        global::CsvForge.CsvGeneratedWriterSupport.WriteEscaped(writer, ").Append(accessor).AppendLine("?.ToString(), options.Delimiter);");
            }

            return;
        }

        var specialType = column.Type.SpecialType;
        if (specialType == SpecialType.System_Int32 || specialType == SpecialType.System_Int64 || specialType == SpecialType.System_Double || column.Type.ToDisplayString() == "System.DateTime")
        {
            source.AppendLine("        Span<char> formatted = stackalloc char[64];");
            if (specialType == SpecialType.System_Int32 || specialType == SpecialType.System_Int64)
            {
                source.Append("        ").Append(accessor).AppendLine(".TryFormat(formatted, out var charsWritten, default, CultureInfo.InvariantCulture);");
            }
            else if (specialType == SpecialType.System_Double)
            {
                source.Append("        ").Append(accessor).AppendLine(".TryFormat(formatted, out var charsWritten, \"G\", CultureInfo.InvariantCulture);");
            }
            else
            {
                source.Append("        ").Append(accessor).AppendLine(".TryFormat(formatted, out var charsWritten, \"O\", CultureInfo.InvariantCulture);");
            }

            if (utf8)
            {
                source.AppendLine("        global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, formatted.Slice(0, charsWritten), (byte)options.Delimiter);");
            }
            else if (isAsync)
            {
                source.AppendLine("        await global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedAsync(writer, formatted.Slice(0, charsWritten).ToString(), options.Delimiter, cancellationToken).ConfigureAwait(false);");
            }
            else
            {
                source.AppendLine("        global::CsvForge.CsvGeneratedWriterSupport.WriteEscaped(writer, formatted.Slice(0, charsWritten), options.Delimiter);");
            }

            return;
        }

        if (column.Type.SpecialType == SpecialType.System_String)
        {
            if (utf8)
            {
                source.Append("        global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, ").Append(accessor).AppendLine(", (byte)options.Delimiter);");
            }
            else if (isAsync)
            {
                source.Append("        await global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedAsync(writer, ").Append(accessor).AppendLine(", options.Delimiter, cancellationToken).ConfigureAwait(false);");
            }
            else
            {
                source.Append("        global::CsvForge.CsvGeneratedWriterSupport.WriteEscaped(writer, ").Append(accessor).AppendLine(", options.Delimiter);");
            }

            return;
        }

        if (utf8)
        {
            source.Append("        global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedUtf8(writer, ").Append(accessor).AppendLine(".ToString(), (byte)options.Delimiter);");
        }
        else if (isAsync)
        {
            source.Append("        await global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedAsync(writer, ").Append(accessor).AppendLine(".ToString(), options.Delimiter, cancellationToken).ConfigureAwait(false);");
        }
        else
        {
            source.Append("        global::CsvForge.CsvGeneratedWriterSupport.WriteEscaped(writer, ").Append(accessor).AppendLine(".ToString(), options.Delimiter);");
        }
    }

    private static string GetWriterBaseName(INamedTypeSymbol symbol)
    {
        var stack = new Stack<string>();
        ISymbol? current = symbol;
        while (current is INamedTypeSymbol named)
        {
            stack.Push(named.Name);
            current = named.ContainingType;
        }

        return string.Join("_", stack);
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private sealed record ColumnModel(string PropertyName, string ColumnName, int? Order, ITypeSymbol Type, bool IsNullable, int DeclarationOrder)
    {
        public ColumnOrderKey SortKey => new(Order, DeclarationOrder, PropertyName);
    }
}
