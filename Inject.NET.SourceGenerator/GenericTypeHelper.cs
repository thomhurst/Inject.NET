using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Inject.NET.SourceGenerator;

internal static class GenericTypeHelper
{
    private static readonly ConcurrentDictionary<Compilation, Dictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>>> _compilationCache = new();
    
    public static IEnumerable<INamedTypeSymbol> GetConstructedTypes(
        Compilation compilation,
        INamedTypeSymbol genericTypeDefinition)
    {
        var cache = _compilationCache.GetOrAdd(compilation, BuildConstructedTypesCache);
        var originalGenericDefinition = genericTypeDefinition.OriginalDefinition;
        
        if (cache.TryGetValue(originalGenericDefinition, out var constructedTypes))
        {
            return constructedTypes;
        }
        
        return Enumerable.Empty<INamedTypeSymbol>();
    }
    
    private static Dictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>> BuildConstructedTypesCache(Compilation compilation)
    {
        var cache = new Dictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>>(SymbolEqualityComparer.Default);
        
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();
            var typeNodes = root.DescendantNodes().OfType<TypeSyntax>();

            foreach (var typeNode in typeNodes)
            {
                if (semanticModel.GetTypeInfo(typeNode).Type 
                        is INamedTypeSymbol { IsGenericType: true } typeSymbol 
                    && !typeSymbol.IsGenericDefinition())
                {
                    var originalDefinition = typeSymbol.OriginalDefinition;
                    
                    if (!cache.TryGetValue(originalDefinition, out var constructedSet))
                    {
                        constructedSet = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                        cache[originalDefinition] = constructedSet;
                    }
                    
                    constructedSet.Add(typeSymbol);
                }
            }
        }
        
        return cache;
    }
}