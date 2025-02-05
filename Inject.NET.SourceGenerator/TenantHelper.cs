using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class TenantHelper
{
    public static Tenant[] ConstructTenants(Compilation compilation,
        AttributeData[] withTenantAttributes, IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> rootDependencies)
    {
        return ConstructTenantsEnumerable(compilation, withTenantAttributes, rootDependencies).ToArray();
    }

    private static IEnumerable<Tenant> ConstructTenantsEnumerable(Compilation compilation, AttributeData[] withTenantAttributes,
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> rootDependencies)
    {
        var dependencyInjectionAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.IDependencyInjectionAttribute");

        foreach (var withTenantAttribute in withTenantAttributes)
        {
            var definitionType = withTenantAttribute.AttributeClass!.TypeArguments[0];
            
            var attributes = definitionType.GetAttributes();
            
            var dependencyAttributes = attributes
                .Where(x => x.AttributeClass?.AllInterfaces.Contains(dependencyInjectionAttributeType,
                    SymbolEqualityComparer.Default) == true)
                .ToArray();

            var dependencies = DependencyDictionary.Create(compilation, dependencyAttributes, definitionType.Name);
            
            yield return new Tenant
            {
                TenantDefinition = (INamedTypeSymbol) definitionType,
                RootDependencies = rootDependencies,
                TenantDependencies = dependencies
            };
        }
    }
}