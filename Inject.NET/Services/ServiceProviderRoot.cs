using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public abstract class ServiceProviderRoot<TSelf, TSingletonScope> : IServiceProviderRoot
    where TSelf : ServiceProviderRoot<TSelf, TSingletonScope>
    where TSingletonScope : SingletonScope
{
    protected readonly ServiceFactories ServiceFactories;

    protected readonly Dictionary<string, IServiceProvider> Tenants = [];
    
    public abstract TSingletonScope SingletonScope { get; }

    public ServiceProviderRoot(ServiceFactories serviceFactories)
    {
        ServiceFactories = serviceFactories;
    }

    protected void Register(string tenant, IServiceProvider provider)
    {
        Tenants[tenant] = provider;
    }

    public virtual async ValueTask InitializeAsync()
    {
        await using var scope = CreateScope();
        
        foreach (var key in ServiceFactories.Descriptors.Keys.Where(x => x.Type.IsConstructedGenericType))
        {
            scope.GetService(key);
        }
        
        foreach (var (_, serviceProvider) in Tenants)
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
        return Tenants[tenantId];
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (_, value) in Tenants)
        {
            await value.DisposeAsync();
        }

        await SingletonScope.DisposeAsync();
    }
}