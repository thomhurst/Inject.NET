using Inject.NET.Enums;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal class TenantedSingletonScope(ServiceProviderRoot root, ServiceFactories serviceFactories) : IServiceScope
{
    private readonly SingletonScope _scope = new(root, serviceFactories);

    public object? GetService(Type type)
    {
        return _scope.GetService(type) ?? root.SingletonScope.GetService(type);
    }

    public IEnumerable<object> GetServices(Type type)
    {
        if (root.TryGetSingletons(type, out var defaultSingletons))
        {
            return
            [
                ..defaultSingletons,
                .._scope.GetServices(type)
            ];
        }

        return _scope.GetServices(type);
    }

    public object? GetService(Type type, string? key)
    {
        return _scope.GetService(type, key) ?? root.SingletonScope.GetService(type, key);
    }

    public IEnumerable<object> GetServices(Type type, string? key)
    {
        if (root.TryGetSingletons(type, key, out var defaultSingletons))
        {
            return
            [
                ..defaultSingletons,
                .._scope.GetServices(type, key)
            ];
        }
        
        return _scope.GetServices(type, key);
    }

    public IServiceProvider Root => _scope.Root;

    public ValueTask DisposeAsync()
    {
        return _scope.DisposeAsync();
    }
    
    public void PreBuild() => _scope.PreBuild();
    public Task FinalizeAsync() => _scope.FinalizeAsync();
}