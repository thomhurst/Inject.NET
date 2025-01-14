using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Services;

namespace Inject.NET.Extensions;

public static class ServiceScopeExtensions
{
    public static T GetRequiredService<T>(this IServiceScope scope) where T : class
    {
        return scope.GetService(new ServiceKey(typeof(T))) as T ??
               ThrowMissingDependencyError<T>(scope);
    }
    
    public static T GetRequiredService<T>(this IServiceScope scope, string key) where T : class
    {
        return scope.GetService(new ServiceKey(typeof(T), key)) as T ??
               ThrowMissingDependencyError<T>(key, scope);
    }
    
    public static IEnumerable<T> GetServices<T>(this IServiceScope scope, string key) where T : class
    {
        var enumerable = scope.GetService(new ServiceKey(typeof(T), key));

        if (enumerable is null)
        {
            return [];
        }

        if (enumerable.GetType().IsIEnumerable())
        {
            return (enumerable as IEnumerable<object>)?.Cast<T>() ?? [];
        }

        if (enumerable is T t)
        {
            return [t];
        }
        
        return [];
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