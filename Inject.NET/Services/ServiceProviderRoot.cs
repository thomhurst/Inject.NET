using System.Collections.Frozen;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Pools;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal class ServiceProviderRoot : IServiceProviderRoot
{
    internal static readonly AsyncLocal<IServiceScope> Scopes = new();
    internal readonly ObjectPool<ServiceScope> ServiceScopePool;

    internal readonly SingletonScope SingletonScope;
    private readonly ServiceFactories _serviceFactories;
    private readonly IDictionary<string, IServiceRegistrar> _tenantRegistrars;

    private FrozenDictionary<string, IServiceProvider> _tenants = null!;

    internal ServiceProviderRoot(ServiceFactories serviceFactories, IDictionary<string, IServiceRegistrar> tenantRegistrars)
    {
        _serviceFactories = serviceFactories;
        _tenantRegistrars = tenantRegistrars;
        SingletonScope = new SingletonScope(this, serviceFactories);
        ServiceScopePool =
            new ObjectPool<ServiceScope>(new ServiceScopePoolPolicy(this, SingletonScope, serviceFactories));
    }

    internal async ValueTask InitializeAsync()
    {
        await BuildTenants();
        
        SingletonScope.PreBuild();
        
        await using var scope = CreateScope();
        
        foreach (var type in _serviceFactories.Descriptors.Keys)
        {
            scope.GetService(type);
        }
        
        await SingletonScope.FinalizeAsync();
    }

    private async Task BuildTenants()
    {
        List<(string Id, IServiceProvider ServiceProvider)> tenants = [];

        foreach (var (key, value) in _tenantRegistrars)
        {
            var serviceProvider = await value.BuildAsync(this);
            tenants.Add((key, serviceProvider));
        }

        _tenants = tenants.ToFrozenDictionary(x => x.Id, x => x.ServiceProvider);
    }
    
    internal bool TryGetSingletons(ServiceKey serviceKey, out IReadOnlyList<object> singletons)
    {
        var foundSingletons = SingletonScope.GetServices(serviceKey).ToArray();
        
        if (foundSingletons.Length > 0)
        {
            singletons = foundSingletons;
            return true;
        }
        
        singletons = [];
        return false;
    }

    public IServiceScope CreateScope()
    {
        return Scopes.Value ??= ServiceScopePool.Get();
    }

    public IServiceProvider GetTenant(string tenantId)
    {
        return _tenants[tenantId];
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (_, value) in _tenants)
        {
            await value.DisposeAsync();
        }

        await SingletonScope.DisposeAsync();
    }
}