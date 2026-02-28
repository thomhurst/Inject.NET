using Inject.NET.Interfaces;
using MsDi = Microsoft.Extensions.DependencyInjection;

namespace Inject.NET.Extensions.DependencyInjection;

internal sealed class ServiceScopeWrapper : MsDi.IServiceScope
{
    private readonly IServiceScope _innerScope;

    public ServiceScopeWrapper(IServiceScope innerScope)
    {
        _innerScope = innerScope;
    }

    public System.IServiceProvider ServiceProvider => _innerScope;

    public void Dispose()
    {
        _innerScope.Dispose();
    }
}
