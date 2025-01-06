using Inject.NET.Models;
using Inject.NET.Services;

namespace Inject.NET.Pools;

internal class ServiceScopePoolPolicy(
    ServiceProviderRoot serviceProviderRoot,
    SingletonScope singletonScope,
    ServiceFactories serviceFactories) : IPooledObjectPolicy<ServiceScope>
{
    public ServiceScope Create()
    {
        return new ServiceScope(serviceProviderRoot, singletonScope, serviceFactories);   
    }

    public async Task<bool> ReturnAsync(ServiceScope obj)
    {
        await obj.DisposeAsync();
        
        return true;
    }
}

internal interface IPooledObjectPolicy<T>
{
    T Create();
    Task<bool> ReturnAsync(T obj);
}