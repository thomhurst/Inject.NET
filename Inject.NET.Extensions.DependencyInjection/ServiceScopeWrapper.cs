using Inject.NET.Interfaces;
using MsDi = Microsoft.Extensions.DependencyInjection;

namespace Inject.NET.Extensions.DependencyInjection;

/// <summary>
/// Adapts an Inject.NET <see cref="IServiceScope"/> to the MEDI
/// <see cref="MsDi.IServiceScope"/> contract. Implements <see cref="IAsyncDisposable"/>
/// in addition to <see cref="IDisposable"/> so that consumers using
/// <c>await using var scope = ...</c> get proper async disposal of scoped instances.
/// </summary>
internal sealed class ServiceScopeWrapper : MsDi.IServiceScope, IAsyncDisposable
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

    public ValueTask DisposeAsync()
    {
        return _innerScope.DisposeAsync();
    }
}
