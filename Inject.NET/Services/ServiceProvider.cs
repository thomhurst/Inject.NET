using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public abstract class ServiceProvider<TSelf, TSingletonScope, TScope, TParentServiceProvider, TParentSingletonScope, TParentServiceScope> : IServiceProviderRoot<TScope>
    where TSelf : ServiceProvider<TSelf, TSingletonScope, TScope, TParentServiceProvider, TParentSingletonScope, TParentServiceScope>
    where TSingletonScope : SingletonScope<TSingletonScope, TSelf, TScope, TParentSingletonScope, TParentServiceScope, TParentServiceProvider>
    where TScope : ServiceScope<TScope, TSelf, TSingletonScope, TParentServiceScope, TParentSingletonScope, TParentServiceProvider>
    where TParentServiceScope : IServiceScope
    where TParentSingletonScope : IServiceScope
{
    public TParentServiceProvider? ParentServiceProvider { get; }
    protected readonly ServiceFactories ServiceFactories;

    protected readonly Dictionary<string, IServiceProvider> Tenants = [];
    
    public abstract TSingletonScope SingletonScope { get; }

    public ServiceProvider(ServiceFactories serviceFactories, TParentServiceProvider? parentServiceProvider)
    {
        ParentServiceProvider = parentServiceProvider;
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
        
        // foreach (var (_, serviceProvider) in Tenants)
        // {
        //     var tenantServiceProvider = serviceProvider;
        //     
        //     await using var tenantScope = tenantServiceProvider.CreateScope();
        //
        //     foreach (var key in tenantServiceProvider.ServiceFactories.Descriptors.Keys.Where(x => x.Type.IsConstructedGenericType))
        //     {
        //         tenantScope.GetService(key);
        //     }
        // }
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

    public abstract TScope CreateTypedScope();

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

    public IServiceScope CreateScope()
    {
        return CreateTypedScope();
    }
}