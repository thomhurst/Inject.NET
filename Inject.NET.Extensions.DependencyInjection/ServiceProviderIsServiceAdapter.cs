using MsDi = Microsoft.Extensions.DependencyInjection;

namespace Inject.NET.Extensions.DependencyInjection;

internal sealed class ServiceProviderIsServiceAdapter : MsDi.IServiceProviderIsService
{
    private readonly Inject.NET.Interfaces.IServiceProvider _provider;

    public ServiceProviderIsServiceAdapter(Inject.NET.Interfaces.IServiceProvider provider)
    {
        _provider = provider;
    }

    public bool IsService(Type serviceType)
    {
        return _provider.IsService(serviceType);
    }
}
