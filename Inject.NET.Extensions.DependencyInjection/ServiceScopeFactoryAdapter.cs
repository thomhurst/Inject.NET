using MsDi = Microsoft.Extensions.DependencyInjection;

namespace Inject.NET.Extensions.DependencyInjection;

internal sealed class ServiceScopeFactoryAdapter : MsDi.IServiceScopeFactory
{
    private readonly Inject.NET.Interfaces.IServiceProvider _provider;

    public ServiceScopeFactoryAdapter(Inject.NET.Interfaces.IServiceProvider provider)
    {
        _provider = provider;
    }

    public MsDi.IServiceScope CreateScope()
    {
        return new ServiceScopeWrapper(_provider.CreateScope());
    }
}
