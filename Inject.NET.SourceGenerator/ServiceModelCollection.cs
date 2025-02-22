﻿using System.Collections.Concurrent;
using Inject.NET.SourceGenerator.Extensions;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

public class RootServiceModelCollection : ServiceModelCollection
{
    public INamedTypeSymbol ServiceProviderType { get; }

    public readonly ConcurrentDictionary<string, TenantServiceModelCollection> Tenants = [];

    public RootServiceModelCollection(INamedTypeSymbol serviceProviderType, ServiceModel[] dependencies, Tenant[] tenants) : base(dependencies, [])
    {
        ServiceProviderType = serviceProviderType;
        
        foreach (var tenant in tenants)
        {
            Tenants.TryAdd(tenant.TenantDefinition.GloballyQualified(),
                new TenantServiceModelCollection(tenant.TenantDefinition, tenant.TenantDependencies.SelectMany(x => x.Value).ToArray(), dependencies));
        }
    }
}

public class TenantServiceModelCollection : ServiceModelCollection
{
    public INamedTypeSymbol TenantDefinition { get; }
    public string TenantName => TenantDefinition.Name;

    public TenantServiceModelCollection(INamedTypeSymbol tenantDefinition, ServiceModel[] dependencies, ServiceModel[] parentDependencies) : base(dependencies, parentDependencies)
    {
        TenantDefinition = tenantDefinition;
    }
}


public abstract class ServiceModelCollection
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

        public override int GetHashCode()
        {
            unchecked
            {
                return (Type.GloballyQualified().GetHashCode() * 397) ^ (Key != null ? Key.GetHashCode() : 0);
            }
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