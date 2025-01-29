using System.Collections.Concurrent;
using Inject.NET.SourceGenerator.Extensions;
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
    public readonly SortedDictionary<ServiceKey, List<ServiceModel>> Services = new();
    
    public ServiceModelCollection(ServiceModel[] dependencies, ServiceModel[] parentDependencies)
    {
        var allDependencies = dependencies.Concat(parentDependencies)
            .GroupBy(x => x.ServiceKey)
            .ToDictionary(x => x.Key, x => x.ToList());
        
        foreach (var parentDependency in parentDependencies)
        {
            Services.GetOrAdd(parentDependency.ServiceKey, []).Add(parentDependency with
            {
                ResolvedFromParent = !parentDependency.GetAllNestedParameters(allDependencies).Select(x => x.ServiceType).Intersect(dependencies.Select(x => x.ServiceType), SymbolEqualityComparer.Default).Any()
            });
        }
        
        foreach (var dependency in dependencies)
        {
            Services.GetOrAdd(dependency.ServiceKey, []).Add(dependency with
            {
                ResolvedFromParent = false
            });
        }
    }

    public sealed record ServiceKey(ITypeSymbol Type, string? Key) : IComparable<ServiceKey>, IComparable
    {
        public bool Equals(ServiceKey? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return SymbolEqualityComparer.Default.Equals(Type, other.Type) && Key == other.Key;
        }

        public int CompareTo(ServiceKey? other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (other is null)
            {
                return 1;
            }

            return string.Compare(
                $"{Type.GloballyQualified()}{Key}",
                $"{other.Type.GloballyQualified()}{other.Key}", StringComparison.Ordinal
            );
        }

        public int CompareTo(object? obj)
        {
            return CompareTo(obj as ServiceKey);
        }
    }
}