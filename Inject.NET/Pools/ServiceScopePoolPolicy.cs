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

    public ValueTask<bool> ReturnAsync(ServiceScope obj)
    {
        var vt = obj.DisposeAsync();

        if (vt.IsCompletedSuccessfully)
        {
            return ValueTask.FromResult(true);
        }
        
        return Await(vt);
    }

    private static async ValueTask<bool> Await(ValueTask vt)
    {
        await vt;
        return true;
    }
}

internal interface IPooledObjectPolicy<T>
{
    T Create();
    ValueTask<bool> ReturnAsync(T obj);
}