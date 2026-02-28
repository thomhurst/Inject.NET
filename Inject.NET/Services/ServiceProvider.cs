using Inject.NET.Enums;
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
    /// Validates the service provider configuration by attempting to resolve all registered services.
    /// Throws an <see cref="AggregateException"/> containing details of all services that failed to resolve.
    /// </summary>
    /// <remarks>
    /// This method creates a temporary scope and attempts to resolve every registered service type.
    /// Open generic type definitions are skipped since they require concrete type arguments.
    /// The method checks singleton, scoped, and transient services.
    /// </remarks>
    /// <exception cref="AggregateException">
    /// Thrown when one or more services fail to resolve. Each inner exception describes
    /// which service type and implementation type failed, along with the underlying error.
    /// </exception>
    public async Task Verify()
    {
        var errors = new List<Exception>();

        await using var scope = CreateTypedScope();

        foreach (var (serviceKey, descriptors) in ServiceFactories.Descriptors)
        {
            // Skip open generic type definitions - they cannot be resolved without concrete type arguments
            if (serviceKey.Type.IsGenericTypeDefinition)
            {
                continue;
            }

            foreach (var descriptor in descriptors.Items)
            {
                try
                {
                    var instance = descriptor.Factory(scope, serviceKey.Type, descriptor.Key);

                    if (instance == null)
                    {
                        errors.Add(new InvalidOperationException(
                            $"Service resolution returned null for service type '{serviceKey.Type.FullName}' " +
                            $"(implementation: '{descriptor.ImplementationType.FullName}', " +
                            $"lifetime: {descriptor.Lifetime}" +
                            (serviceKey.Key != null ? $", key: '{serviceKey.Key}'" : "") + ")."));
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new InvalidOperationException(
                        $"Failed to resolve service type '{serviceKey.Type.FullName}' " +
                        $"(implementation: '{descriptor.ImplementationType.FullName}', " +
                        $"lifetime: {descriptor.Lifetime}" +
                        (serviceKey.Key != null ? $", key: '{serviceKey.Key}'" : "") + "): " +
                        ex.Message, ex));
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException(
                $"Service provider verification failed with {errors.Count} error(s).", errors);
        }
    }

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
    /// Determines whether a service of the specified type is available from the service provider.
    /// </summary>
    /// <param name="serviceType">The type of service to check</param>
    /// <returns>True if the service type is registered; otherwise, false</returns>
    public bool IsService(Type serviceType)
    {
        if (serviceType == Types.ServiceScope
            || serviceType == Types.ServiceProvider
            || serviceType == Types.SystemServiceProvider)
        {
            return true;
        }

        // Check if the type is registered in the service factories
        var serviceKey = new ServiceKey(serviceType);

        if (ServiceFactories.Descriptors.ContainsKey(serviceKey))
        {
            return true;
        }

        // Check for open generic registrations
        if (serviceType.IsConstructedGenericType
            && ServiceFactories.Descriptors.ContainsKey(new ServiceKey(serviceType.GetGenericTypeDefinition())))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a child container that inherits all registrations from this provider
    /// but can add or override registrations independently.
    /// Child singletons are independent from the parent, and scoped/transient instances
    /// are always created fresh within the child's scopes.
    /// </summary>
    /// <param name="configure">An action to configure additional or overriding registrations for the child container</param>
    /// <returns>A new child service provider</returns>
    public ChildServiceProvider CreateChildContainer(Action<IServiceRegistrar> configure)
    {
        var registrar = new ChildContainerRegistrar();
        configure(registrar);
        return new ChildServiceProvider(this, ServiceFactories, registrar.ServiceFactoryBuilders);
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