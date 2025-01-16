using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface IServiceScope : IAsyncDisposable, IDisposable, System.IServiceProvider
{
    object? GetService(ServiceKey serviceKey);
    IReadOnlyList<object> GetServices(ServiceKey serviceKey);
    
    object? GetService(ServiceKey serviceKey, IServiceScope originatingScope);
    IReadOnlyList<object> GetServices(ServiceKey serviceKey, IServiceScope originatingScope);
}

public interface IServiceScope<TSelf, out TServiceProvider, out TSingletonScope> : IServiceScope
    where TServiceProvider : IServiceProvider<TSelf>
    where TSingletonScope : IServiceScope
    where TSelf : IServiceScope<TSelf, TServiceProvider, TSingletonScope>
{
    TSingletonScope Singletons { get; }
    TServiceProvider ServiceProvider { get; }
}