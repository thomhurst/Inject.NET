using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Helpers;

/// <summary>
/// Helper class for creating Lazy&lt;T&gt; instances at runtime when resolving via GetService(Type).
/// </summary>
/// <typeparam name="T">The inner service type</typeparam>
internal class LazyFactory<T> where T : class
{
    private readonly IServiceScope _scope;

    public LazyFactory(IServiceScope scope)
    {
        _scope = scope;
    }

    public Lazy<T> Create()
    {
        return new Lazy<T>(() => (T)(_scope.GetService(new ServiceKey(typeof(T)))
            ?? throw new InvalidOperationException($"Service of type {typeof(T).FullName} is not registered.")));
    }
}
