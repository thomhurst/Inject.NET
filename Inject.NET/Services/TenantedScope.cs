using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

internal class TenantedScope(IServiceScope defaultScope, ServiceFactories serviceFactories) : ServiceScope((ServiceProvider) defaultScope.RootServiceProvider, serviceFactories)
{
    public override IEnumerable<object> GetServices(Type type)
    {
        return
        [
            defaultScope.GetServices(type),
            ..base.GetServices(type)
        ];
    }

    public override IEnumerable<object> GetServices(Type type, string key)
    {
        return
        [
            defaultScope.GetServices(type, key),
            ..base.GetServices(type, key)
        ];
    }

    public override async ValueTask DisposeAsync()
    {
        await defaultScope.DisposeAsync();
        await base.DisposeAsync();
    }
}