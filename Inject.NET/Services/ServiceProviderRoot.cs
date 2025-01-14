using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

public abstract class ServiceProviderRoot<TSelf, TSingletonScope, TScope> : IServiceProviderRoot<TScope>
    where TSelf : ServiceProviderRoot<TSelf, TSingletonScope, TScope>
    where TSingletonScope : SingletonScope<TSingletonScope, TSelf, TScope>
    where TScope : ServiceScope<TSelf, TSingletonScope, TScope>
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
            var tenantServiceProvider = (TenantServiceProvider<TScope>)serviceProvider;
            
            await using var tenantScope = tenantServiceProvider.CreateScope();

            foreach (var key in tenantServiceProvider.ServiceFactories.Descriptors.Keys.Where(x => x.Type.IsConstructedGenericType))
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
    
    public IServiceProvider GetTenant(string tenantId)
    {
        return Tenants[tenantId];
    }

    public abstract TScope CreateScope();

    public async ValueTask DisposeAsync()
    {
        foreach (var (_, value) in Tenants)
        {
            if(value is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
        }

        await SingletonScope.DisposeAsync();
    }

    public object? GetService(Type serviceType)
    {
        return CreateScope().GetService(new ServiceKey(serviceType));
    }
}