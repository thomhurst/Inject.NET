using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Inject.NET.SourceGenerator;

public static class GenericTypeHelper
{
    public static IEnumerable<INamedTypeSymbol> GetConstructedTypes(
        Compilation compilation,
        INamedTypeSymbol genericTypeDefinition)
    {
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            
            var root = syntaxTree.GetRoot();

            var typeNodes = root.DescendantNodes().OfType<TypeSyntax>();

            foreach (var typeNode in typeNodes)
            {
                if (semanticModel.GetTypeInfo(typeNode).Type 
                        is INamedTypeSymbol { IsGenericType: true } typeSymbol 
                    && !typeSymbol.IsGenericDefinition()
                    && SymbolEqualityComparer.Default.Equals(typeSymbol.OriginalDefinition, genericTypeDefinition))
                {
                    yield return typeSymbol;
                }
            }
        }
    }
}