using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface IServiceScope : IAsyncDisposable, IDisposable, System.IServiceProvider
{
    object? GetService(ServiceKey serviceKey);
    IReadOnlyList<object> GetServices(ServiceKey serviceKey);
}

public interface IServiceScope<TSelf, out TServiceProvider, out TSingletonScope> : IServiceScope
    where TServiceProvider : IServiceProvider<TSelf>
    where TSingletonScope : IServiceScope
    where TSelf : IServiceScope<TSelf, TServiceProvider, TSingletonScope>
{
    TSingletonScope SingletonScope { get; }
    TServiceProvider ServiceProvider { get; }
}