using System.Collections.Concurrent;
using System.Collections.Frozen;
using Inject.NET.Enums;
using Inject.NET.Helpers;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal sealed class SingletonScope(IServiceProvider root, ServiceFactories serviceFactories) : IServiceScope
{
    private static readonly Type ServiceScopeType = typeof(IServiceScope);
    private static readonly Type ServiceProviderType = typeof(IServiceProvider);

    private readonly ConcurrentDictionary<CacheKey, List<object>> _singletonsBuilder = [];
    private FrozenDictionary<CacheKey, FrozenSet<object>> _singletonEnumerables = FrozenDictionary<CacheKey, FrozenSet<object>>.Empty;
    private FrozenDictionary<CacheKey, object> _singletons = FrozenDictionary<CacheKey, object>.Empty;

    private readonly ConcurrentDictionary<CacheKey, object[]> _openGenericSingletons = [];

    
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
            d => d.Value.ToFrozenSet()
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
        if (type == ServiceScopeType)
        {
            return this;
        }
        
        if (type == ServiceProviderType)
        {
            return ServiceProvider;
        }
        
        return GetService(new CacheKey(type));
    }

    private object? GetService(CacheKey cacheKey)
    {
        if (_singletons.TryGetValue(cacheKey, out var singleton))
        {
            return singleton;
        }
        
        return GetServices(cacheKey).LastOrDefault();
    }
    
    public IEnumerable<object> GetServices(Type type)
    {
        return GetServices(new CacheKey(type));
    }
    
    public IEnumerable<object> GetServices(CacheKey cacheKey)
    {
        if (_singletonEnumerables.TryGetValue(cacheKey, out var list))
        {
            return list;
        }
        
        if (_openGenericSingletons.TryGetValue(cacheKey, out var openGenericSingletons))
        {
            return openGenericSingletons;
        }

        if (!_isBuilt)
        {
            if (_singletonsBuilder.TryGetValue(cacheKey, out var cache))
            {
                return cache;
            }

            return _singletonsBuilder[cacheKey] =
            [
                ..SingletonFactories(cacheKey)
                    .Select(descriptor => Constructor.Construct(this, cacheKey.Type, descriptor))
            ];
        }

        var type = cacheKey.Type;
        if (type.IsGenericType && SingletonFactories(new CacheKey(type.GetGenericTypeDefinition())) is {} genericTypeFactories)
        {
            return _openGenericSingletons[cacheKey] =
            [
                ..genericTypeFactories.OfType<OpenGenericServiceDescriptor>()
                    .Select(descriptor => Constructor.Construct(this, type, descriptor))
            ];
        }

        return [];
    }

    private IEnumerable<IServiceDescriptor> SingletonFactories(CacheKey cacheKey)
    {
        return serviceFactories.Descriptors.Where(x => x.Key == cacheKey)
            .SelectMany(x => x.Value)
            .Where(x => x.Lifetime == Lifetime.Singleton);
    }

    public object? GetService(Type type, string? key)
    {
        return GetService(new CacheKey(type, key));
    }

    public IEnumerable<object> GetServices(Type type, string? key)
    {
        return GetServices(new CacheKey(type));
    }

    internal IEnumerable<CacheKey> GetSingletonKeys()
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
}