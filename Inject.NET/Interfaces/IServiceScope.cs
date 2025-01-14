using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface IServiceScope : IAsyncDisposable, IDisposable, System.IServiceProvider
{
    object? GetService(ServiceKey serviceKey);
    IReadOnlyList<object> GetServices(ServiceKey serviceKey);
}

public interface IServiceScope<out TServiceProvider, out TSingletonScope> : IServiceScope
    where TServiceProvider : IServiceProvider
    where TSingletonScope : IServiceScope
{
    TSingletonScope SingletonScope { get; }
    TServiceProvider ServiceProvider { get; }
}