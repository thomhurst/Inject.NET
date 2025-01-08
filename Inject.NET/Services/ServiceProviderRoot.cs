using System.Collections.Frozen;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Pools;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public class ServiceProviderRoot : IServiceProviderRoot
{
    internal readonly ObjectPool<ServiceScope> ServiceScopePool;

    internal readonly SingletonScope SingletonScope;
    private readonly ServiceFactories _serviceFactories;
    private readonly IDictionary<string, IServiceRegistrar> _tenantRegistrars;

    private FrozenDictionary<string, IServiceProvider> _tenants = null!;

    public ServiceProviderRoot(ServiceFactories serviceFactories, IDictionary<string, IServiceRegistrar> tenantRegistrars)
    {
        _serviceFactories = serviceFactories;
        _tenantRegistrars = tenantRegistrars;
        SingletonScope = new SingletonScope(this, serviceFactories);
        ServiceScopePool =
            new ObjectPool<ServiceScope>(new ServiceScopePoolPolicy(this, SingletonScope, serviceFactories));
    }

    public async ValueTask InitializeAsync()
    {
        await BuildTenants();
        
        SingletonScope.PreBuild();
        
        foreach (var (_, serviceProvider) in _tenants)
        {
            await using var tenantScope = serviceProvider.CreateScope();

            foreach (var key in ((TenantServiceProvider)serviceProvider).ServiceFactories.Descriptors.Keys.Where(x => x.Type.IsConstructedGenericType))
            {
                tenantScope.GetService(key);
            }
        }
        
        await using var scope = CreateScope();
        
        foreach (var key in _serviceFactories.Descriptors.Keys.Where(x => x.Type.IsConstructedGenericType))
        {
            scope.GetService(key);
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
        
        singletons = Array.Empty<object>();
        return false;
    }

    public IServiceScope CreateScope()
    {
        return ServiceScopePool.Get();
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