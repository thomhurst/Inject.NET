using Inject.NET.Models;

namespace Inject.NET.Services;

internal class TenantedSingletonScope(ServiceProviderRoot root, ServiceFactories serviceFactories) : SingletonScope(root, serviceFactories)
{
    public override IEnumerable<object> GetServices(Type type)
    {
        if (root.TryGetSingletons(type, out var defaultSingletons))
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
        if (root.TryGetSingletons(type, key, out var defaultSingletons))
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