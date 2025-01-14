using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface IServiceScope : IAsyncDisposable, IDisposable, IServiceProvider
{
    object? GetService(ServiceKey serviceKey);
    IReadOnlyList<object> GetServices(ServiceKey serviceKey);
}

public interface IServiceScope<out TServiceProvider, out TSingletonScope, TScope> : IServiceScope
    where TServiceProvider : IServiceProvider<TScope>
    where TSingletonScope : IServiceScope
    where TScope : IServiceScope
{
    TSingletonScope SingletonScope { get; }
    TServiceProvider ServiceProvider { get; }
}