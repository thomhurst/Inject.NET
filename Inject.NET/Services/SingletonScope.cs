using System.Collections.Concurrent;
using System.Collections.Frozen;
using Inject.NET.Enums;
using Inject.NET.Helpers;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal class SingletonScope(IServiceProvider serviceProvider, ServiceFactories serviceFactories) : IServiceScope
{
    private readonly ConcurrentDictionary<Type, List<object>> _singletonsBuilder = [];
    private FrozenDictionary<Type, FrozenSet<object>> _singletons = FrozenDictionary<Type, FrozenSet<object>>.Empty;
    
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, List<object>>> _keyedSingletonsBuilder = [];
    private FrozenDictionary<Type, FrozenDictionary<string, FrozenSet<object>>> _keyedSingletons =
        FrozenDictionary<Type, FrozenDictionary<string, FrozenSet<object>>>.Empty;

    private bool _isBuilt;
    
    public IServiceProvider ServiceProvider { get; } = serviceProvider;

    public async ValueTask BuildAsync()
    {
        foreach (var type in GetSingletonTypes())
        {
            GetServices(type);
        }

        foreach (var (type, key) in GetKeyedSingletonTypes())
        {
            GetServices(type, key);
        }

        _singletons = _singletonsBuilder.ToFrozenDictionary(
            d => d.Key,
            d => d.Value.ToFrozenSet()
        );
        
        _keyedSingletons = _keyedSingletonsBuilder.ToFrozenDictionary(
            outerDictionary => outerDictionary.Key,
            outerDictionary => outerDictionary.Value.ToFrozenDictionary(
                innerDictionary => innerDictionary.Key,
                innerDictionary => innerDictionary.Value.ToFrozenSet()
            )
        );

        IEnumerable<object> allSingletons =
        [
            _singletons.SelectMany(s => s.Value),
            _keyedSingletons.SelectMany(s => s.Value)
                .Select(x => x.Value),
        ];

        foreach (var singletonAsyncInitialization in allSingletons
                     .OfType<ISingletonAsyncInitialization>()
                     .OrderBy(x => x.Order))
        {
            await singletonAsyncInitialization.InitializeAsync();
        }

        _isBuilt = true;
    }

    public object? GetService(Type type)
    {
        return GetServices(type).LastOrDefault();
    }

    public virtual IEnumerable<object> GetServices(Type type)
    {
        if (_singletons.TryGetValue(type, out var list))
        {
            return list;
        }

        if (!_isBuilt)
        {
            if (_singletonsBuilder.TryGetValue(type, out var cache))
            {
                return cache;
            }
            
            return _singletonsBuilder[type] = [..SingletonFactories(type).Select(func => func(this, type))];
        }

        return [];
    }

    private IEnumerable<Func<IServiceScope, Type, object>> SingletonFactories(Type type)
    {
        return serviceFactories.Factories.Where(x => x.Key == type)
            .SelectMany(x => x.Value)
            .Where(x => x.Item1 == Lifetime.Singleton)
            .Select(x => x.Item2);
    }
    
    private IEnumerable<Func<IServiceScope, Type, string, object>> KeyedSingletonFactories(Type type, string key)
    {
        return serviceFactories.KeyedFactories.Where(x => x.Key == type)
            .SelectMany(x => x.Value)
            .Where(x => x.Key == key)
            .SelectMany(x => x.Value)
            .Where(x => x.Item1 == Lifetime.Singleton)
            .Select(x => x.Item2);
    }

    public object? GetService(Type type, string key)
    {
        return GetServices(type, key).LastOrDefault();
    }

    public virtual IEnumerable<object> GetServices(Type type, string key)
    {
        if (_keyedSingletons.TryGetValue(type, out var innerDictionary)
            && innerDictionary.TryGetValue(key, out var list))
        {
            return list;
        }

        if (!_isBuilt)
        {
            if (_keyedSingletonsBuilder.TryGetValue(type, out var cachedDictionary)
                && cachedDictionary.TryGetValue(key, out var cache))
            {
                return cache;
            }
            
            return _keyedSingletonsBuilder.GetOrAdd(type, [])[key] =
                [..KeyedSingletonFactories(type, key).Select(func => func(this, type, key))];
        }

        return [];
    }
    
    private IEnumerable<Type> GetSingletonTypes()
    {
        return serviceFactories.Factories
            .Where(x => x.Value.Items.Any(y => y.Item1 == Lifetime.Singleton))
            .GroupBy(x => x.Key)
            .Select(x => x.Key);
    }
    
    private IEnumerable<(Type Type, string Key)> GetKeyedSingletonTypes()
    {
        foreach (var typeGroups in serviceFactories.KeyedFactories
                     .Where(x => x.Value.Values.Any(y => y.Items.Any(z => z.Item1 == Lifetime.Singleton)))
                     .GroupBy(x => x.Key))
        {
            foreach (var keyGroups in typeGroups.SelectMany(x => x.Value)
                         .GroupBy(x => x.Key))
            {
                yield return (typeGroups.Key, keyGroups.Key);
            }
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        foreach (var item in _singletons.SelectMany(x => x.Value)
                     .Concat(_keyedSingletons.SelectMany(x => x.Value)
                         .SelectMany(x => x.Value)))
        {
            await Disposer.DisposeAsync(item);
        }
    }
}

internal class TenantedSingletonScope(ServiceProvider serviceProvider, ServiceFactories serviceFactories) : SingletonScope(serviceProvider, serviceFactories)
{
    public override IEnumerable<object> GetServices(Type type)
    {
        if (serviceProvider.TryGetSingletons(type, out var defaultSingletons))
        {
            return
            [
                ..defaultSingletons,
                ..base.GetServices(type)
            ];
        }

        return base.GetServices(type);
    }

    public override IEnumerable<object> GetServices(Type type, string key)
    {
        if (serviceProvider.TryGetSingletons(type, key, out var defaultSingletons))
        {
            return
            [
                ..defaultSingletons,
                ..base.GetServices(type, key)
            ];
        }
        
        return base.GetServices(type, key);
    }
}