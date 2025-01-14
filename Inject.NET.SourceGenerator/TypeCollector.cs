using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

public class TypeCollector
{
    public static TenantedServiceProviderInformation Collect(TypedServiceProviderModel serviceProviderModel, Compilation compilation)
    {
        var dependencyInjectionAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.IDependencyInjectionAttribute");

        var withTenantAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.WithTenantAttribute`1");
        
        var attributes = serviceProviderModel.Type
            .GetAttributes();
        
        var dependencyAttributes = attributes
            .Where(x => x.AttributeClass?.AllInterfaces.Contains(dependencyInjectionAttributeType,
                SymbolEqualityComparer.Default) == true)
            .ToArray();
        
        var withTenantAttributes = attributes
            .Where(x => x.AttributeClass?.IsGenericType is true && SymbolEqualityComparer.Default.Equals(withTenantAttributeType, x.AttributeClass))
            .ToArray();

        var rootDependencies = DependencyDictionary.Create(compilation, dependencyAttributes);

        var tenants = TenantHelper.ConstructTenants(compilation, withTenantAttributes, rootDependencies);
        
        return new TenantedServiceProviderInformation(serviceProviderModel.Type, rootDependencies, tenants);
    }
}

public record TenantedServiceProviderInformation(INamedTypeSymbol ServiceProviderType, Dictionary<ISymbol?,ServiceModel[]> RootDependencies, Tenant[] Tenants) : ServiceProviderInformation(ServiceProviderType, RootDependencies)
{
    public Dictionary<Tenant, ServiceProviderInformation> TenantDependencies { get; } =
        Tenants.ToDictionary(t => t, t => new ServiceProviderInformation(ServiceProviderType, t.TenantDependencies)
        {
            ParentDependencies = RootDependencies
        });
}

public record ServiceProviderInformation
{
    public ServiceProviderInformation(INamedTypeSymbol serviceProviderType,
        Dictionary<ISymbol?, ServiceModel[]> dependencies)
    {
        ServiceProviderType = serviceProviderType;
        Dependencies = dependencies;
        
        Enumerables = dependencies.ToDictionary(
            serviceTypes => serviceTypes.Key,
            keyValuePair => keyValuePair.Value.ToArray(),
            SymbolEqualityComparer.Default);
        
        Singular = dependencies.ToDictionary(
            serviceTypes => serviceTypes.Key,
            keyValuePair => keyValuePair.Value[^1],
            SymbolEqualityComparer.Default);
    }

    public Dictionary<ISymbol?, ServiceModel[]> ParentDependencies { get; init; } = [];
    
    public Dictionary<ISymbol?, ServiceModel[]> Enumerables { get; }

    public Dictionary<ISymbol?, ServiceModel> Singular { get; }

    public INamedTypeSymbol ServiceProviderType { get; init; }
    public Dictionary<ISymbol?, ServiceModel[]> Dependencies { get; init; }
}