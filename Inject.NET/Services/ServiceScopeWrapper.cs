using Inject.NET.Interfaces;
using MsDi = Microsoft.Extensions.DependencyInjection;

namespace Inject.NET.Services;

/// <summary>
/// Wraps an Inject.NET <see cref="IServiceScope"/> to implement the
/// <see cref="MsDi.IServiceScope"/> interface from Microsoft.Extensions.DependencyInjection.
/// This enables interoperability with ASP.NET Core and other libraries that depend on
/// <see cref="MsDi.IServiceScopeFactory"/>.
/// </summary>
internal sealed class ServiceScopeWrapper : MsDi.IServiceScope
{
    private readonly IServiceScope _innerScope;

    public ServiceScopeWrapper(IServiceScope innerScope)
    {
        _innerScope = innerScope;
    }

    /// <summary>
    /// Gets the service provider associated with this scope.
    /// Returns the inner scope itself, which implements <see cref="System.IServiceProvider"/>.
    /// </summary>
    public System.IServiceProvider ServiceProvider => _innerScope;

    /// <summary>
    /// Disposes the underlying scope.
    /// </summary>
    public void Dispose()
    {
        _innerScope.Dispose();
    }
}
