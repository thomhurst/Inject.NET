using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class ModuleHelper
{
    /// <summary>
    /// Finds all [UseModule&lt;T&gt;] attributes on the given type and recursively collects
    /// all attributes from referenced module types.
    /// Returns the combined attributes from all modules (excluding [UseModule] attributes themselves).
    /// </summary>
    public static AttributeData[] CollectModuleAttributes(
        ImmutableArray<AttributeData> sourceAttributes,
        Compilation compilation)
    {
        var useModuleAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.UseModuleAttribute`1");

        if (useModuleAttributeType is null)
        {
            return [];
        }

        var useModuleAttributes = sourceAttributes
            .Where(x => x.AttributeClass?.IsGenericType is true
                && SymbolEqualityComparer.Default.Equals(useModuleAttributeType, x.AttributeClass.OriginalDefinition))
            .ToArray();

        if (useModuleAttributes.Length == 0)
        {
            return [];
        }

        var visited = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var result = new List<AttributeData>();

        foreach (var useModuleAttribute in useModuleAttributes)
        {
            var moduleType = useModuleAttribute.AttributeClass!.TypeArguments[0];
            CollectFromModule(moduleType, useModuleAttributeType, compilation, visited, result);
        }

        return result.ToArray();
    }

    private static void CollectFromModule(
        ITypeSymbol moduleType,
        INamedTypeSymbol useModuleAttributeType,
        Compilation compilation,
        HashSet<ITypeSymbol> visited,
        List<AttributeData> result)
    {
        if (!visited.Add(moduleType))
        {
            // Already visited — skip to avoid cycles
            return;
        }

        var moduleAttributes = moduleType.GetAttributes();

        foreach (var attr in moduleAttributes)
        {
            if (attr.AttributeClass is null)
            {
                continue;
            }

            if (attr.AttributeClass.IsGenericType
                && SymbolEqualityComparer.Default.Equals(useModuleAttributeType, attr.AttributeClass.OriginalDefinition))
            {
                continue;
            }

            result.Add(attr);
        }

        foreach (var attr in moduleAttributes)
        {
            if (attr.AttributeClass?.IsGenericType is true
                && SymbolEqualityComparer.Default.Equals(useModuleAttributeType, attr.AttributeClass.OriginalDefinition))
            {
                var nestedModuleType = attr.AttributeClass.TypeArguments[0];
                CollectFromModule(nestedModuleType, useModuleAttributeType, compilation, visited, result);
            }
        }
    }
}
