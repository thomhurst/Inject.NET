using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class DecoratorDictionary
{
    /// <summary>
    /// Creates a dictionary of decorators from the provided attribute data.
    /// </summary>
    /// <param name="compilation">The compilation context for type resolution.</param>
    /// <param name="decoratorAttributes">Array of decorator attributes to process.</param>
    /// <param name="tenantName">Optional tenant name for multi-tenant scenarios.</param>
    /// <returns>A dictionary mapping service keys to lists of decorator models.</returns>
    public static IDictionary<ServiceModelCollection.ServiceKey, List<DecoratorModel>> Create(
        Compilation compilation,
        AttributeData[] decoratorAttributes,
        string? tenantName)
    {
        var decorators = new Dictionary<ServiceModelCollection.ServiceKey, List<DecoratorModel>>();
        
        foreach (var attributeData in decoratorAttributes)
        {
            if (attributeData.AttributeClass is null)
                continue;
            
            var baseClass = attributeData.AttributeClass.BaseType;
            if (baseClass is null || baseClass.Name != "DecoratorAttribute")
                continue;
            
            // Extract service type and decorator type
            INamedTypeSymbol? serviceType = null;
            INamedTypeSymbol? decoratorType = null;
            
            // Check if it's a generic decorator attribute
            if (attributeData.AttributeClass.IsGenericType && attributeData.AttributeClass.TypeArguments.Length == 2)
            {
                serviceType = attributeData.AttributeClass.TypeArguments[0] as INamedTypeSymbol;
                decoratorType = attributeData.AttributeClass.TypeArguments[1] as INamedTypeSymbol;
            }
            else if (attributeData.ConstructorArguments.Length >= 2)
            {
                serviceType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
                decoratorType = attributeData.ConstructorArguments[1].Value as INamedTypeSymbol;
            }
            
            if (serviceType is null || decoratorType is null)
                continue;
            
            // Extract lifetime from attribute name
            var lifetime = ExtractLifetime(attributeData.AttributeClass.Name);
            
            // Extract Order property if present
            int order = 0;
            string? key = null;
            foreach (var namedArg in attributeData.NamedArguments)
            {
                if (namedArg.Key == "Order")
                {
                    order = (int)(namedArg.Value.Value ?? 0);
                }
                else if (namedArg.Key == "Key")
                {
                    key = namedArg.Value.Value as string;
                }
            }
            
            // Get constructor parameters for the decorator
            var constructors = decoratorType.Constructors
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
                    IsEnumerable = false,
                    Key = null
                })
                .ToArray();
            
            var decoratorModel = new DecoratorModel
            {
                ServiceType = serviceType,
                DecoratorType = decoratorType,
                Lifetime = lifetime,
                Order = order,
                Parameters = parameters,
                Key = key,
                TenantName = tenantName
            };
            
            var serviceKey = new ServiceModelCollection.ServiceKey(serviceType, key);
            if (!decorators.ContainsKey(serviceKey))
            {
                decorators[serviceKey] = new List<DecoratorModel>();
            }
            
            decorators[serviceKey].Add(decoratorModel);
        }
        
        // Sort decorators by order
        foreach (var decoratorList in decorators.Values)
        {
            decoratorList.Sort((a, b) => a.Order.CompareTo(b.Order));
        }
        
        return decorators;
    }
    
    private static Lifetime ExtractLifetime(string attributeName)
    {
        if (attributeName.StartsWith("SingletonDecorator"))
            return Lifetime.Singleton;
        if (attributeName.StartsWith("ScopedDecorator"))
            return Lifetime.Scoped;
        if (attributeName.StartsWith("TransientDecorator"))
            return Lifetime.Transient;
        
        // Default to transient if not specified
        return Lifetime.Transient;
    }
}