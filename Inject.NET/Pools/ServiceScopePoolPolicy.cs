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

    public Task<bool> ReturnAsync(ServiceScope obj)
    {
        var vt = obj.DisposeAsync();

        if (vt.IsCompletedSuccessfully)
        {
            return Task.FromResult(true);
        }
        
        return vt.AsTask().ContinueWith(_ => true);
    }
}

internal interface IPooledObjectPolicy<T>
{
    T Create();
    Task<bool> ReturnAsync(T obj);
}