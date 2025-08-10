using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface IServiceScope : IAsyncDisposable, IDisposable, System.IServiceProvider
{
    object? GetService(ServiceKey serviceKey);
    IReadOnlyList<object> GetServices(ServiceKey serviceKey);
    
    object? GetService(ServiceKey serviceKey, IServiceScope originatingScope);
    IReadOnlyList<object> GetServices(ServiceKey serviceKey, IServiceScope originatingScope);
}

/// <summary>
/// Represents a typed service scope with strongly-typed access to service provider and singleton scope.
/// Provides compile-time type safety for scope operations.
/// </summary>
/// <typeparam name="TSelf">The concrete service scope type</typeparam>
/// <typeparam name="TServiceProvider">The service provider type</typeparam>
/// <typeparam name="TSingletonScope">The singleton scope type</typeparam>
public interface IServiceScope<TSelf, out TServiceProvider, out TSingletonScope> : IServiceScope
    where TServiceProvider : IServiceProvider<TSelf>
    where TSingletonScope : IServiceScope
    where TSelf : IServiceScope<TSelf, TServiceProvider, TSingletonScope>
{
    /// <summary>
    /// Gets the singleton scope for accessing singleton services.
    /// </summary>
    TSingletonScope Singletons { get; }
    
    /// <summary>
    /// Gets the service provider that created this scope.
    /// </summary>
    TServiceProvider ServiceProvider { get; }
}