using System.Collections.Concurrent;
using System.Collections.Frozen;
using Inject.NET.Enums;
using Inject.NET.Helpers;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal class SingletonScope(IServiceProvider rootServiceProvider, ServiceFactories serviceFactories) : IServiceScope
{
    private readonly ConcurrentDictionary<Type, List<object>> _singletonsBuilder = [];
    private FrozenDictionary<Type, FrozenSet<object>> _singletons = FrozenDictionary<Type, FrozenSet<object>>.Empty;
    
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, List<object>>> _keyedSingletonsBuilder = [];
    private FrozenDictionary<Type, FrozenDictionary<string, FrozenSet<object>>> _keyedSingletons =
        FrozenDictionary<Type, FrozenDictionary<string, FrozenSet<object>>>.Empty;

    private readonly ConcurrentDictionary<Type, object[]> _openGenericSingletons = [];
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, object[]>> _openGenericKeyedSingletons = [];

    
    private bool _isBuilt;
    
    public IServiceProvider RootServiceProvider { get; } = rootServiceProvider;

    public void PreBuild()
    {
        foreach (var type in GetSingletonTypes())
        {
            GetServices(type);
        }

        foreach (var (type, key) in GetKeyedSingletonTypes())
        {
            GetServices(type, key);
        }
    }

    internal async Task FinalizeAsync()
    {
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
        
        if (_openGenericSingletons.TryGetValue(type, out var openGenericSingletons))
        {
            return openGenericSingletons;
        }

        if (!_isBuilt)
        {
            if (_singletonsBuilder.TryGetValue(type, out var cache))
            {
                return cache;
            }

            return _singletonsBuilder[type] =
            [
                ..SingletonFactories(type)
                    .Select(descriptor => Constructor.Construct(this, type, null, descriptor))
            ];
        }

        if (type.IsGenericType && SingletonFactories(type.GetGenericTypeDefinition()) is {} genericTypeFactories)
        {
            return _openGenericSingletons[type] =
            [
                ..genericTypeFactories.OfType<OpenGenericServiceDescriptor>()
                    .Select(descriptor => Constructor.Construct(this, type, null, descriptor))
            ];
        }

        return [];
    }

    private IEnumerable<IServiceDescriptor> SingletonFactories(Type type)
    {
        return serviceFactories.Factories.Where(x => x.Key == type)
            .SelectMany(x => x.Value)
            .Where(x => x.Lifetime == Lifetime.Singleton);
    }
    
    private IEnumerable<IKeyedServiceDescriptor> KeyedSingletonFactories(Type type, string key)
    {
        return serviceFactories.KeyedFactories.Where(x => x.Key == type)
            .SelectMany(x => x.Value)
            .Where(x => x.Key == key)
            .SelectMany(x => x.Value)
            .Where(x => x.Lifetime == Lifetime.Singleton);
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
        
        if (_keyedSingletons.TryGetValue(type, out var openGenericInnerDictionary)
            && openGenericInnerDictionary.TryGetValue(key, out var openGenericSingletons))
        {
            return openGenericSingletons;
        }

        if (!_isBuilt)
        {
            if (_keyedSingletonsBuilder.TryGetValue(type, out var cachedDictionary)
                && cachedDictionary.TryGetValue(key, out var cache))
            {
                return cache;
            }
            
            return _keyedSingletonsBuilder.GetOrAdd(type, [])[key] =
                [..KeyedSingletonFactories(type, key).Select(descriptor => Constructor.Construct(this, type, key, descriptor))];
        }
        
        
        if (type.IsGenericType && KeyedSingletonFactories(type.GetGenericTypeDefinition(), key) is {} genericTypeFactories)
        {
            return _openGenericKeyedSingletons.GetOrAdd(type, [])[key] =
            [
                ..genericTypeFactories.OfType<OpenGenericKeyedServiceDescriptor>()
                    .Select(descriptor => Constructor.Construct(this, type, null, descriptor))
            ];
        }

        return [];
    }
    
    private IEnumerable<Type> GetSingletonTypes()
    {
        return serviceFactories.Factories
            .Where(x => x.Value.Items.Any(y => y.Lifetime == Lifetime.Singleton))
            .GroupBy(x => x.Key)
            .Select(x => x.Key)
            .Where(x => !x.IsGenericTypeDefinition);
    }
    
    private IEnumerable<(Type Type, string Key)> GetKeyedSingletonTypes()
    {
        foreach (var typeGroups in serviceFactories.KeyedFactories
                     .Where(x => x.Value.Values.Any(y => y.Items.Any(z => z.Lifetime == Lifetime.Singleton)))
                     .GroupBy(x => x.Key)
                     .Where(x => !x.Key.IsGenericTypeDefinition)
        )
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