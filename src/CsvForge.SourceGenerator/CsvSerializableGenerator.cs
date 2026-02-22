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
        context.RegisterSourceOutput(compilationAndCandidates, static (spc, source) => Execute(spc, source.Left, source.Right));
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
            if (attributeClass is null)
            {
                continue;
            }

            if (attributeClass.ToDisplayString() == "CsvForge.Attributes.CsvSerializableAttribute")
            {
                return symbol;
            }
        }

        return null;
    }

    private static void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<INamedTypeSymbol> candidates)
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
            EmitWriter(context, symbol, columns);
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

    private static void EmitWriter(SourceProductionContext context, INamedTypeSymbol symbol, List<ColumnModel> columns)
    {
        var ns = symbol.ContainingNamespace.IsGlobalNamespace ? null : symbol.ContainingNamespace.ToDisplayString();
        var targetType = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var writerTypeName = GetWriterTypeName(symbol);
        var source = new StringBuilder();
        source.AppendLine("using System;");
        source.AppendLine("using System.IO;");
        source.AppendLine("using System.Threading;");
        source.AppendLine("using System.Threading.Tasks;");
        source.AppendLine("using System.Runtime.CompilerServices;");

        if (ns is not null)
        {
            source.Append("namespace ").Append(ns).AppendLine(";");
            source.AppendLine();
        }

        source.Append("file sealed class ").Append(writerTypeName).Append(" : global::CsvForge.ICsvTypeWriter<").Append(targetType).AppendLine(">")
            .AppendLine("{")
            .Append("    public static readonly ").Append(writerTypeName).AppendLine(" Instance = new();")
            .AppendLine()
            .AppendLine("    public void WriteHeader(TextWriter writer, global::CsvForge.CsvOptions options)")
            .AppendLine("    {");

        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
            {
                source.AppendLine("        writer.Write(options.Delimiter);");
            }

            source.Append("        global::CsvForge.CsvGeneratedWriterSupport.WriteEscaped(writer, \"").Append(Escape(columns[i].ColumnName)).AppendLine("\", options.Delimiter);");
        }

        source.AppendLine("    }")
            .AppendLine()
            .AppendLine("    public void WriteRow(TextWriter writer, " + targetType + " value, global::CsvForge.CsvOptions options)")
            .AppendLine("    {");

        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
            {
                source.AppendLine("        writer.Write(options.Delimiter);");
            }

            AppendValueWrite(source, columns[i], isAsync: false);
        }

        source.AppendLine("    }")
            .AppendLine()
            .AppendLine("    public async ValueTask WriteHeaderAsync(TextWriter writer, global::CsvForge.CsvOptions options, CancellationToken cancellationToken)")
            .AppendLine("    {");

        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
            {
                source.AppendLine("        await writer.WriteAsync(options.Delimiter).ConfigureAwait(false);");
            }

            source.Append("        await global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedAsync(writer, \"").Append(Escape(columns[i].ColumnName)).AppendLine("\", options.Delimiter, cancellationToken).ConfigureAwait(false);");
        }

        source.AppendLine("    }")
            .AppendLine()
            .AppendLine("    public async ValueTask WriteRowAsync(TextWriter writer, " + targetType + " value, global::CsvForge.CsvOptions options, CancellationToken cancellationToken)")
            .AppendLine("    {");

        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
            {
                source.AppendLine("        cancellationToken.ThrowIfCancellationRequested();");
                source.AppendLine("        await writer.WriteAsync(options.Delimiter).ConfigureAwait(false);");
            }

            AppendValueWrite(source, columns[i], isAsync: true);
        }

        source.AppendLine("    }")
            .AppendLine("}");



        var hintName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty).Replace(".", "_").Replace("+", "_") + "_CsvWriter.g.cs";
        context.AddSource(hintName, SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private static void AppendValueWrite(StringBuilder source, ColumnModel column, bool isAsync)
    {
        var accessor = "value." + column.PropertyName;
        var textExpr = column.Type.SpecialType == SpecialType.System_String
            ? accessor
            : $"{accessor}.ToString()";

        if (column.IsNullable)
        {
            source.Append("        var ").Append(column.PropertyName).Append("Value = ").Append(accessor).AppendLine(";");
            source.Append("        ");
            if (isAsync)
            {
                source.Append("await global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedAsync(writer, ").Append(column.PropertyName).Append("Value?.ToString(), options.Delimiter, cancellationToken).ConfigureAwait(false);");
            }
            else
            {
                source.Append("global::CsvForge.CsvGeneratedWriterSupport.WriteEscaped(writer, ").Append(column.PropertyName).Append("Value?.ToString(), options.Delimiter);");
            }

            source.AppendLine();
            return;
        }

        if (isAsync)
        {
            source.Append("        await global::CsvForge.CsvGeneratedWriterSupport.WriteEscapedAsync(writer, ").Append(textExpr).AppendLine(", options.Delimiter, cancellationToken).ConfigureAwait(false);");
            return;
        }

        source.Append("        global::CsvForge.CsvGeneratedWriterSupport.WriteEscaped(writer, ").Append(textExpr).AppendLine(", options.Delimiter);");
    }

    private static string GetWriterTypeName(INamedTypeSymbol symbol)
    {
        var stack = new Stack<string>();
        ISymbol? current = symbol;
        while (current is INamedTypeSymbol named)
        {
            stack.Push(named.Name);
            current = named.ContainingType;
        }

        return string.Join("_", stack) + "_CsvWriter";
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private sealed record ColumnModel(string PropertyName, string ColumnName, int? Order, ITypeSymbol Type, bool IsNullable, int DeclarationOrder)
    {
        public ColumnOrderKey SortKey => new(Order, DeclarationOrder, PropertyName);
    }

}
