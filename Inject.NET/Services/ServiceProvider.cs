using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

/// <summary>
/// Base class for service providers that manage dependency injection with compile-time optimization.
/// Provides service creation, scope management, and tenant support for the Inject.NET library.
/// </summary>
/// <typeparam name="TSelf">The concrete service provider type</typeparam>
/// <typeparam name="TSingletonScope">The singleton scope type</typeparam>
/// <typeparam name="TScope">The service scope type</typeparam>
/// <typeparam name="TParentServiceProvider">The parent service provider type</typeparam>
/// <typeparam name="TParentSingletonScope">The parent singleton scope type</typeparam>
/// <typeparam name="TParentServiceScope">The parent service scope type</typeparam>
public abstract class ServiceProvider<TSelf, TSingletonScope, TScope, TParentServiceProvider, TParentSingletonScope, TParentServiceScope> : IServiceProviderRoot<TScope>
    where TSelf : ServiceProvider<TSelf, TSingletonScope, TScope, TParentServiceProvider, TParentSingletonScope, TParentServiceScope>
    where TSingletonScope : SingletonScope<TSingletonScope, TSelf, TScope, TParentSingletonScope, TParentServiceScope, TParentServiceProvider>
    where TScope : ServiceScope<TScope, TSelf, TSingletonScope, TParentServiceScope, TParentSingletonScope, TParentServiceProvider>
    where TParentServiceScope : IServiceScope
    where TParentSingletonScope : IServiceScope
{
    /// <summary>
    /// Gets the parent service provider, if this is a nested provider.
    /// </summary>
    public TParentServiceProvider? ParentServiceProvider { get; }
    protected readonly ServiceFactories ServiceFactories;

    protected readonly Dictionary<Type, IServiceProvider> Tenants = [];
    
    /// <summary>
    /// Gets the singleton scope that manages all singleton services.
    /// </summary>
    public abstract TSingletonScope Singletons { get; }

    /// <summary>
    /// Initializes a new instance of the service provider.
    /// </summary>
    /// <param name="serviceFactories">The service factories for creating instances</param>
    /// <param name="parentServiceProvider">The optional parent service provider</param>
    public ServiceProvider(ServiceFactories serviceFactories, TParentServiceProvider? parentServiceProvider)
    {
        ParentServiceProvider = parentServiceProvider;
        ServiceFactories = serviceFactories;
    }

    /// <summary>
    /// Registers a tenant-specific service provider.
    /// </summary>
    /// <typeparam name="TTenant">The tenant type</typeparam>
    /// <param name="provider">The service provider for this tenant</param>
    protected void Register<TTenant>(IServiceProvider provider)
    {
        Tenants[typeof(TTenant)] = provider;
    }

    /// <summary>
    /// Initializes the service provider by pre-creating generic type instances.
    /// This helps with performance by resolving generic types at startup.
    /// </summary>
    /// <returns>A task representing the initialization operation</returns>
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
        var foundSingletons = Singletons.GetServices(serviceKey).ToArray();
        
        if (foundSingletons.Length > 0)
        {
            singletons = foundSingletons;
            return true;
        }
        
        singletons = Array.Empty<object>();
        return false;
    }
    
    /// <summary>
    /// Gets the service provider for a specific tenant.
    /// </summary>
    /// <typeparam name="TTenant">The tenant type</typeparam>
    /// <returns>The service provider for the specified tenant</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the tenant is not registered</exception>
    public IServiceProvider GetTenant<TTenant>()
    {
        return Tenants[typeof(TTenant)];
    }

    /// <summary>
    /// Creates a new typed service scope for dependency resolution.
    /// </summary>
    /// <returns>A new service scope instance</returns>
    public abstract TScope CreateTypedScope();

    /// <summary>
    /// Asynchronously disposes the service provider and all its resources.
    /// </summary>
    /// <returns>A task representing the disposal operation</returns>
    public async ValueTask DisposeAsync()
    {
        foreach (var (_, value) in Tenants)
        {
            if(value is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
        }

        await Singletons.DisposeAsync();
    }

    /// <summary>
    /// Gets a service instance of the specified type.
    /// </summary>
    /// <param name="serviceType">The type of service to retrieve</param>
    /// <returns>The service instance, or null if not found</returns>
    public object? GetService(Type serviceType)
    {
        return CreateScope().GetService(new ServiceKey(serviceType));
    }

    /// <summary>
    /// Creates a new service scope for managing scoped services.
    /// </summary>
    /// <returns>A new service scope instance</returns>
    public IServiceScope CreateScope()
    {
        return CreateTypedScope();
    }
}