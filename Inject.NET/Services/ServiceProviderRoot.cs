using System.Collections.Frozen;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal class ServiceProviderRoot : IServiceProviderRoot
{
    private readonly SingletonScope _singletonScope;
    private readonly ServiceFactories _serviceFactories;
    private readonly IDictionary<string, IServiceRegistrar> _tenantRegistrars;

    private FrozenDictionary<string, IServiceProvider> _tenants = null!;

    internal ServiceProviderRoot(ServiceFactories serviceFactories, IDictionary<string, IServiceRegistrar> tenantRegistrars)
    {
        _serviceFactories = serviceFactories;
        _tenantRegistrars = tenantRegistrars;
        _singletonScope = new SingletonScope(this, serviceFactories);
    }

    internal async ValueTask InitializeAsync()
    {
        await BuildTenants();
        
        _singletonScope.PreBuild();
        
        await using var scope = CreateScope();
        
        foreach (var type in _serviceFactories.EnumerableDescriptors.Keys)
        {
            scope.GetServices(type);
        }
        
        foreach (var (type, keyedFactory) in _serviceFactories.KeyedEnumerableDescriptors)
        {
            foreach (var key in keyedFactory.Keys)
            {
                scope.GetServices(type, key);
            }
        }
        
        await _singletonScope.FinalizeAsync();
    }

    private async Task BuildTenants()
    {
        List<(string Id, IServiceProvider ServiceProvider)> tenants = [];

        foreach (var (key, value) in _tenantRegistrars)
        {
            var serviceProvider = await value.BuildAsync(this);
            tenants.Add((key, serviceProvider));
        }

        _tenants = tenants.ToFrozenDictionary(x => x.Id, x => x.ServiceProvider);
    }
    
    internal object? GetSingleton(Type type)
    {
        return _singletonScope.GetService(type);
    }
    
    internal object? GetSingleton(Type type, string key)
    {
        return _singletonScope.GetService(type, key);
    }

    internal bool TryGetSingletons(Type type, out IReadOnlyList<object> singletons)
    {
        var foundSingletons = _singletonScope.GetServices(type).ToArray();
        if (foundSingletons.Length > 0)
        {
            singletons = foundSingletons;
            return true;
        }
        
        singletons = [];
        return false;
    }
    
    internal bool TryGetSingletons(Type type, string key, out IReadOnlyList<object> singletons)
    {
        var foundSingletons = _singletonScope.GetServices(type, key).ToArray();
        
        if (foundSingletons.Length > 0)
        {
            singletons = foundSingletons;
            return true;
        }
        
        singletons = [];
        return false;
    }

    public IServiceScope CreateScope()
    {
        return new ServiceScope(this, _singletonScope, _serviceFactories);
    }

    public IServiceProvider GetTenant(string tenantId)
    {
        return _tenants[tenantId];
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (_, value) in _tenants)
        {
            await value.DisposeAsync();
        }

        await _singletonScope.DisposeAsync();
    }
}