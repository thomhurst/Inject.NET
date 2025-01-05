using Inject.NET.Models;

namespace Inject.NET.Services;

internal class TenantedSingletonScope(ServiceProviderRoot rootServiceProviderRoot, ServiceFactories serviceFactories) : SingletonScope(rootServiceProviderRoot, serviceFactories)
{
    public override IEnumerable<object> GetServices(Type type)
    {
        if (rootServiceProviderRoot.TryGetSingletons(type, out var defaultSingletons))
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
        if (rootServiceProviderRoot.TryGetSingletons(type, key, out var defaultSingletons))
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