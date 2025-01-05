using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

internal sealed class TenantedScope(IServiceScope defaultScope, IServiceScope singletonScope, ServiceFactories serviceFactories) : IServiceScope
{
    private readonly ServiceScope _scope = new((ServiceProviderRoot)defaultScope.Root, singletonScope, serviceFactories);

    public object? GetService(Type type)
    {
        return _scope.GetService(type);
    }

    public IEnumerable<object> GetServices(Type type)
    {
        return
        [
            defaultScope.GetServices(type),
            .._scope.GetServices(type)
        ];
    }

    public object? GetService(Type type, string? key)
    {
        return _scope.GetService(type, key);
    }

    public IEnumerable<object> GetServices(Type type, string? key)
    {
        return
        [
            defaultScope.GetServices(type, key),
            .._scope.GetServices(type, key)
        ];
    }

    public IServiceProvider Root => _scope.Root;

    public async ValueTask DisposeAsync()
    {
        await defaultScope.DisposeAsync();
        await _scope.DisposeAsync();
    }
}