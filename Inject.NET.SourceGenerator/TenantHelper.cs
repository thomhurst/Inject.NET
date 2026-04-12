using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class TenantHelper
{
    public static Tenant[] ConstructTenants(Compilation compilation,
        AttributeData[] withTenantAttributes, IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> rootDependencies,
        List<Diagnostic>? diagnostics = null)
    {
        return ConstructTenantsEnumerable(compilation, withTenantAttributes, rootDependencies, diagnostics).ToArray();
    }

    private static IEnumerable<Tenant> ConstructTenantsEnumerable(Compilation compilation, AttributeData[] withTenantAttributes,
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> rootDependencies, List<Diagnostic>? diagnostics)
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

            var dependencies = DependencyDictionary.Create(compilation, dependencyAttributes, definitionType.Name, null, diagnostics);
            
            yield return new Tenant
            {
                TenantDefinition = (INamedTypeSymbol) definitionType,
                RootDependencies = rootDependencies,
                TenantDependencies = dependencies
            };
        }
    }
}