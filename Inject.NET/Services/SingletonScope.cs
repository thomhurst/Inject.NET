using System.Collections.Concurrent;
using Inject.NET.Enums;
using Inject.NET.Extensions;
using Inject.NET.Helpers;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

public class SingletonScope<TSelf, TServiceProvider, TScope, TParentSingletonScope, TParentServiceScope, TParentServiceProvider>(TServiceProvider serviceProvider, ServiceFactories serviceFactories, TParentSingletonScope? parentScope) : IServiceScope, ISingleton
where TServiceProvider : ServiceProvider<TServiceProvider, TSelf, TScope, TParentServiceProvider, TParentSingletonScope, TParentServiceScope>
where TSelf : SingletonScope<TSelf, TServiceProvider, TScope, TParentSingletonScope, TParentServiceScope, TParentServiceProvider>
where TScope : ServiceScope<TScope, TServiceProvider, TSelf, TParentServiceScope, TParentSingletonScope, TParentServiceProvider>
where TParentSingletonScope : IServiceScope
where TParentServiceScope : IServiceScope
{
    public TSelf Singletons => (TSelf)this;
    public TParentSingletonScope? ParentScope { get; } = parentScope;

    protected readonly List<object?> ConstructedObjects = [];
    
    public TServiceProvider ServiceProvider { get; } = serviceProvider;
    
    public T Register<T>(T value)
    {
        ConstructedObjects.Add(value);

        return value;
    }

    public async Task InitializeAsync()
    {
        foreach (var singletonAsyncInitialization in ConstructedObjects
                     .OfType<ISingletonAsyncInitialization>()
                     .OrderBy(x => x.Order))
        {
            await singletonAsyncInitialization.InitializeAsync();
        }
    }

    public object? GetService(Type type)
    {
        var serviceKey = new ServiceKey(type);
        
        if (type.IsIEnumerable())
        {
            return GetServices(serviceKey);
        }

        return GetService(serviceKey);
    }

    public object? GetService(ServiceKey serviceKey)
    {
        return GetService(serviceKey, this);
    }

    public IReadOnlyList<object> GetServices(ServiceKey serviceKey)
    {
        return GetServices(serviceKey, this);
    }

    public virtual object? GetService(ServiceKey serviceKey, IServiceScope originatingScope)
    {
        if (serviceKey.Type == Types.ServiceScope)
        {
            return this;
        }
        
        if (serviceKey.Type == Types.ServiceProvider)
        {
            return ServiceProvider;
        }

        var services = GetServices(serviceKey);

        if (services.Count == 0)
        {
            return ParentScope?.GetService(serviceKey, originatingScope);
        }
        
        return services[^1];
    }

    public virtual IReadOnlyList<object> GetServices(ServiceKey serviceKey, IServiceScope originatingScope)
    {
        // TODO;
        return [];
    }

    private IEnumerable<ServiceKey> GetSingletonKeys()
    {
        return serviceFactories.Descriptors
            .Where(x => x.Value.Items.Any(y => y.Lifetime == Lifetime.Singleton))
            .GroupBy(x => x.Key)
            .Select(x => x.Key)
            .Where(x => !x.Type.IsGenericTypeDefinition);
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var item in ConstructedObjects)
        {
            await Disposer.DisposeAsync(item);
        }
    }

    public void Dispose()
    {
        foreach (var item in ConstructedObjects)
        {
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
            else if (item is IAsyncDisposable asyncDisposable)
            {
                _ = asyncDisposable.DisposeAsync();
            }
        }
    }
}