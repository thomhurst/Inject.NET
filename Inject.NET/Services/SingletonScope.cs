using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using Inject.NET.Enums;
using Inject.NET.Extensions;
using Inject.NET.Helpers;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal sealed class SingletonScope(IServiceProvider root, ServiceFactories serviceFactories) : IServiceScope
{
    private static readonly Type ServiceScopeType = typeof(IServiceScope);
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);

    private readonly ConcurrentDictionary<ServiceKey, List<object>> _singletonsBuilder = [];
    private FrozenDictionary<ServiceKey, ImmutableArray<object>> _singletonEnumerables = FrozenDictionary<ServiceKey, ImmutableArray<object>>.Empty;
    private FrozenDictionary<ServiceKey, object> _singletons = FrozenDictionary<ServiceKey, object>.Empty;
    
    private bool _isBuilt;
    
    public IServiceProvider ServiceProvider { get; } = root;

    public void PreBuild()
    {
        foreach (var cacheKey in GetSingletonKeys())
        {
            GetServices(cacheKey);
        }
    }

    internal async Task FinalizeAsync()
    {
        _singletonEnumerables = _singletonsBuilder.ToFrozenDictionary(
            d => d.Key,
            d => d.Value.ToImmutableArray()
        );

        foreach (var singletonAsyncInitialization in _singletonEnumerables.SelectMany(s => s.Value)
                     .OfType<ISingletonAsyncInitialization>()
                     .OrderBy(x => x.Order))
        {
            await singletonAsyncInitialization.InitializeAsync();
        }

        _singletons = _singletonEnumerables.ToFrozenDictionary(
            x => x.Key,
            x => x.Value.Last()
        );

        _isBuilt = true;
    }

    public object? GetService(Type type)
    {
        if (type.IsIEnumerable())
        {
            return GetServices(new ServiceKey(type));
        }
        
        return GetService(new ServiceKey(type));
    }

    public object? GetService(ServiceKey serviceKey)
    {
        if (_singletons.TryGetValue(serviceKey, out var singleton))
        {
            return singleton;
        }
        
        if (serviceKey.Type == ServiceScopeType)
        {
            return this;
        }
        
        if (serviceKey.Type == ServiceProviderType)
        {
            return ServiceProvider;
        }

        var services = GetServices(serviceKey);

        if (services.Count == 0)
        {
            return null;
        }
        
        return services[^1];
    }

    public IReadOnlyList<object> GetServices(ServiceKey serviceKey)
    {
        if (_singletonEnumerables.TryGetValue(serviceKey, out var list))
        {
            return list;
        }

        if (!_isBuilt)
        {
            if (_singletonsBuilder.TryGetValue(serviceKey, out var cache))
            {
                return cache;
            }

            return _singletonsBuilder[serviceKey] =
            [
                ..SingletonFactories(serviceKey)
                    .Select(descriptor => descriptor.Factory(this, serviceKey.Type, descriptor.Key))
            ];
        }

        return [];
    }

    IServiceScope IServiceScope.SingletonScope => this;

    private IEnumerable<ServiceDescriptor> SingletonFactories(ServiceKey serviceKey)
    {
        return serviceFactories.Descriptors.Where(x => x.Key == serviceKey)
            .SelectMany(x => x.Value)
            .Where(x => x.Lifetime == Lifetime.Singleton);
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
        foreach (var item in _singletonEnumerables)
        {
            foreach (var obj in item.Value)
            {
                await Disposer.DisposeAsync(obj);
            }
        }
    }

    public void Dispose()
    {
        foreach (var item in _singletonEnumerables)
        {
            foreach (var obj in item.Value)
            {
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                else if (obj is IAsyncDisposable asyncDisposable)
                {
                    _ = asyncDisposable.DisposeAsync();
                }
            }
        }
    }
}