using Inject.NET.Models;

namespace Inject.NET.Services;

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