using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Extensions;

public static class ServiceScopeExtensions
{
    public static T GetRequiredService<T>(this IServiceScope scope) where T : class
    {
        return scope.GetService(new ServiceKey(typeof(T))) as T ??
               ThrowMissingDependencyError<T>(scope);
    }
    
    public static object GetRequiredService(this IServiceScope scope, Type type)
    {
        return scope.GetService(type) ??
               ThrowMissingDependencyError<object>(scope);
    }
    
    public static T GetRequiredService<T>(this IServiceScope scope, string key) where T : class
    {
        return scope.GetService(new ServiceKey(typeof(T), key)) as T ??
               ThrowMissingDependencyError<T>(key, scope);
    }

    public static IEnumerable<T> GetServices<T>(this IServiceScope scope) where T : class =>
        GetServices<T>(scope, null);
    
    public static IEnumerable<T> GetServices<T>(this IServiceScope scope, string? key) where T : class
    {
        return scope.GetServices(new ServiceKey(typeof(T), key)).OfType<T>();
    }
    
    public static T? GetOptionalService<T>(this IServiceScope scope) where T : class
    {
        return scope.GetService(new ServiceKey(typeof(T))) as T;
    }
    
    public static T? GetOptionalService<T>(this IServiceScope scope, string key) where T : class
    {
        return scope.GetService(new ServiceKey(typeof(T), key)) as T;
    }
    
    private static T ThrowMissingDependencyError<T>(IServiceScope scope) where T : class
    {
        if (scope is ISingleton)
        {
            throw new ArgumentException($"No singleton registered for type {typeof(T).FullName}. Transient and Scoped dependencies cannot be injected into a singleton.");
        }
        
        throw new ArgumentException($"No services registered for type {typeof(T).FullName}.");
    }
    
    private static T ThrowMissingDependencyError<T>(string key, IServiceScope scope) where T : class
    {
        if (scope is IScoped)
        {
            throw new ArgumentException($"No singleton registered for type {typeof(T).FullName} with key {key}. Transient and Scoped dependencies cannot be injected into a singleton");
        }
        
        throw new ArgumentException($"No services registered for type {typeof(T).FullName} with key {key}.");
    }
}