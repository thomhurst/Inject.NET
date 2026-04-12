using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

public class TypeCollector
{
    public static RootServiceModelCollection Collect(TypedServiceProviderModel serviceProviderModel, Compilation compilation)
    {
        var dependencyInjectionAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.IDependencyInjectionAttribute");

        var withTenantAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.WithTenantAttribute`1");

        var attributes = serviceProviderModel.Type
            .GetAttributes();

        var moduleAttributes = ModuleHelper.CollectModuleAttributes(attributes, compilation);
        var allAttributes = moduleAttributes.Length > 0
            ? attributes.AddRange(moduleAttributes)
            : attributes;

        var dependencyAttributes = allAttributes
            .Where(x => x.AttributeClass?.AllInterfaces.Contains(dependencyInjectionAttributeType,
                SymbolEqualityComparer.Default) == true)
            .ToArray();

        var withTenantAttributes = allAttributes
            .Where(x => x.AttributeClass?.IsGenericType is true && SymbolEqualityComparer.Default.Equals(withTenantAttributeType, x.AttributeClass.OriginalDefinition))
            .ToArray();

        var rootDependencies = DependencyDictionary.Create(compilation, dependencyAttributes, null, serviceProviderModel.Type);

        var tenants = TenantHelper.ConstructTenants(compilation, withTenantAttributes, rootDependencies);

        return new RootServiceModelCollection(serviceProviderModel.Type, rootDependencies.SelectMany(x => x.Value).ToArray(), tenants);
    }
}