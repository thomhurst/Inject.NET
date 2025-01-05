using Inject.NET.Models;

namespace Inject.NET.Services;

internal class TenantedSingletonScope(ServiceProvider rootServiceProvider, ServiceFactories serviceFactories) : SingletonScope(rootServiceProvider, serviceFactories)
{
    public override IEnumerable<object> GetServices(Type type)
    {
        if (rootServiceProvider.TryGetSingletons(type, out var defaultSingletons))
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
        if (rootServiceProvider.TryGetSingletons(type, key, out var defaultSingletons))
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