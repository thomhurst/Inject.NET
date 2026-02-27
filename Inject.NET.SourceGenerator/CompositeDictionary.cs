using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class CompositeDictionary
{
    /// <summary>
    /// Creates a dictionary of composites from the provided attribute data.
    /// </summary>
    /// <param name="compilation">The compilation context for type resolution.</param>
    /// <param name="compositeAttributes">Array of composite attributes to process.</param>
    /// <param name="tenantName">Optional tenant name for multi-tenant scenarios.</param>
    /// <returns>A dictionary mapping service keys to composite models.</returns>
    public static IDictionary<ServiceModelCollection.ServiceKey, CompositeModel> Create(
        Compilation compilation,
        AttributeData[] compositeAttributes,
        string? tenantName)
    {
        var composites = new Dictionary<ServiceModelCollection.ServiceKey, CompositeModel>();

        foreach (var attributeData in compositeAttributes)
        {
            if (attributeData.AttributeClass is null)
                continue;

            // Extract service type and composite type
            INamedTypeSymbol? serviceType = null;
            INamedTypeSymbol? compositeType = null;

            // Check if it's a generic composite attribute (CompositeAttribute<TService, TComposite>)
            if (attributeData.AttributeClass.IsGenericType && attributeData.AttributeClass.TypeArguments.Length == 2)
            {
                serviceType = attributeData.AttributeClass.TypeArguments[0] as INamedTypeSymbol;
                compositeType = attributeData.AttributeClass.TypeArguments[1] as INamedTypeSymbol;
            }
            else if (attributeData.ConstructorArguments.Length >= 2)
            {
                // Non-generic: Composite(typeof(IService), typeof(CompositeService))
                serviceType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
                compositeType = attributeData.ConstructorArguments[1].Value as INamedTypeSymbol;
            }

            if (serviceType is null || compositeType is null)
                continue;

            // Extract Key property if present
            string? key = null;
            foreach (var namedArg in attributeData.NamedArguments)
            {
                if (namedArg.Key == "Key")
                {
                    key = namedArg.Value.Value as string;
                }
            }

            // Get constructor parameters for the composite
            var constructors = compositeType.Constructors
                .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
                .OrderByDescending(c => c.Parameters.Length)
                .ToArray();

            if (constructors.Length == 0)
                continue;

            var constructor = constructors[0];
            var parameters = constructor.Parameters
                .Select(p => new Parameter
                {
                    Type = p.Type,
                    DefaultValue = p.HasExplicitDefaultValue ? p.ExplicitDefaultValue : null,
                    IsOptional = p.IsOptional,
                    IsNullable = p.NullableAnnotation == NullableAnnotation.Annotated,
                    IsEnumerable = CheckIsEnumerable(p.Type, compilation),
                    IsLazy = CheckIsLazy(p.Type, out var lazyInnerType),
                    LazyInnerType = lazyInnerType,
                    IsFunc = CheckIsFunc(p.Type, out var funcInnerType),
                    FuncInnerType = funcInnerType,
                    Key = null
                })
                .ToArray();

            var compositeModel = new CompositeModel
            {
                ServiceType = serviceType,
                CompositeType = compositeType,
                Parameters = parameters,
                Key = key,
                TenantName = tenantName
            };

            var serviceKey = new ServiceModelCollection.ServiceKey(serviceType, key);
            composites[serviceKey] = compositeModel;
        }

        return composites;
    }

    private static bool CheckIsEnumerable(ITypeSymbol parameterType, Compilation compilation)
    {
        var enumerableType = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);

        if (parameterType is INamedTypeSymbol { IsGenericType: true } namedType &&
            SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, enumerableType))
        {
            return true;
        }

        foreach (var interfaceType in parameterType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, enumerableType))
            {
                return true;
            }
        }
        return false;
    }

    private static bool CheckIsLazy(ITypeSymbol parameterType, out ITypeSymbol? innerType)
    {
        if (parameterType is INamedTypeSymbol { IsGenericType: true, Arity: 1 } namedType
            && namedType.ConstructedFrom.ToDisplayString() == "System.Lazy<T>")
        {
            innerType = namedType.TypeArguments[0];
            return true;
        }

        innerType = null;
        return false;
    }

    private static bool CheckIsFunc(ITypeSymbol parameterType, out ITypeSymbol? innerType)
    {
        innerType = null;

        if (parameterType is INamedTypeSymbol { IsGenericType: true } namedType
            && namedType.TypeArguments.Length == 1
            && namedType.OriginalDefinition.ToDisplayString() == "System.Func<TResult>")
        {
            innerType = namedType.TypeArguments[0];
            return true;
        }

        return false;
    }
}
