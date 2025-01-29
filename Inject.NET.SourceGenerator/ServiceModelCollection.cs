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
        var allDependencies = dependencies.Concat(parentDependencies)
            .GroupBy(x => x.ServiceType, SymbolEqualityComparer.Default)
            .ToDictionary(x => x.Key, x => x.ToList(), SymbolEqualityComparer.Default);
        
        foreach (var parentDependency in parentDependencies)
        {
            Services.GetOrAdd(parentDependency.ServiceType, []).Add(parentDependency with
            {
                ResolvedFromParent = !parentDependency.GetAllNestedParameters(allDependencies).Select(x => x.ServiceType).Intersect(dependencies.Select(x => x.ServiceType), SymbolEqualityComparer.Default).Any()
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