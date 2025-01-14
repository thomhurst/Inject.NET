using System.Collections.Frozen;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Pools;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public abstract class ServiceProviderRoot<TSelf, TSingletonScope> : IServiceProviderRoot
    where TSelf : ServiceProviderRoot<TSelf, TSingletonScope>
    where TSingletonScope : SingletonScope
{
    protected readonly ServiceFactories ServiceFactories;
    private readonly IDictionary<string, IServiceRegistrar> _tenantRegistrars;

    private Dictionary<string, IServiceProvider> _tenants = [];
    
    public abstract TSingletonScope SingletonScope { get; }

    public ServiceProviderRoot(ServiceFactories serviceFactories, IDictionary<string, IServiceRegistrar> tenantRegistrars)
    {
        ServiceFactories = serviceFactories;
        _tenantRegistrars = tenantRegistrars;
    }

    public virtual async ValueTask InitializeAsync()
    {
        await using var scope = CreateScope();
        
        foreach (var key in ServiceFactories.Descriptors.Keys.Where(x => x.Type.IsConstructedGenericType))
        {
            scope.GetService(key);
        }
        
        foreach (var (_, serviceProvider) in _tenants)
        {
            await using var tenantScope = serviceProvider.CreateScope();

            foreach (var key in ((TenantServiceProvider<TSelf, TSingletonScope>)serviceProvider).ServiceFactories.Descriptors.Keys.Where(x => x.Type.IsConstructedGenericType))
            {
                tenantScope.GetService(key);
            }
        }
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

    public abstract IServiceScope CreateScope();

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