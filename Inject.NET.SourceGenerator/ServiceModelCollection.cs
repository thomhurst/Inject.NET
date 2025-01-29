using System.Collections.Concurrent;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

public class TenantedServiceModelCollection : ServiceModelCollection
{
    public INamedTypeSymbol ServiceProviderType { get; }

    public readonly ConcurrentDictionary<string, ServiceModelCollection> Tenants = [];

    public TenantedServiceModelCollection(INamedTypeSymbol serviceProviderType, ServiceModel[] dependencies, Tenant[] tenants) : base(dependencies, [])
    {
        ServiceProviderType = serviceProviderType;
        
        foreach (var tenant in tenants)
        {
            Tenants.TryAdd(tenant.TenantDefinition.GloballyQualified(),
                new ServiceModelCollection(tenant.TenantDependencies.SelectMany(x => x.Value).ToArray(), dependencies));
        }
    }
}

public class ServiceModelCollection
{
    public readonly ConcurrentDictionary<ISymbol?, List<ServiceModel>> Services = new(SymbolEqualityComparer.Default);
    
    public ServiceModelCollection(ServiceModel[] dependencies, ServiceModel[] parentDependencies)
    {
        foreach (var parentDependency in parentDependencies)
        {
            Services.GetOrAdd(parentDependency.ServiceType, []).Add(parentDependency with
            {
                ResolvedFromParent = true
            });
        }
        
        foreach (var dependency in dependencies)
        {
            Services.GetOrAdd(dependency.ServiceType, []).Add(dependency with
            {
                ResolvedFromParent = false
            });
        }
    }
}