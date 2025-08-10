using Inject.NET.Constants;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Extensions;

/// <summary>
/// Provides extension methods for service scope operations, including service retrieval and type-safe access.
/// </summary>
public static class ServiceScopeExtensions
{
    /// <summary>
    /// Gets a required service of type T from the service scope.
    /// Throws an exception if the service is not registered.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <param name="scope">The service scope to retrieve from</param>
    /// <returns>The service instance</returns>
    /// <exception cref="ArgumentException">Thrown when the service is not registered</exception>
    /// <example>
    /// <code>
    /// using var scope = serviceProvider.CreateScope();
    /// var myService = scope.GetRequiredService&lt;IMyService&gt;();
    /// </code>
    /// </example>
    public static T GetRequiredService<T>(this IServiceScope scope) where T : class
    {
        return scope.GetService(new ServiceKey(typeof(T))) as T ??
               ThrowMissingDependencyError<T>(scope);
    }
    
    /// <summary>
    /// Gets a required service of the specified type from the service scope.
    /// Throws an exception if the service is not registered.
    /// </summary>
    /// <param name="scope">The service scope to retrieve from</param>
    /// <param name="type">The type of service to retrieve</param>
    /// <returns>The service instance</returns>
    /// <exception cref="ArgumentException">Thrown when the service is not registered</exception>
    public static object GetRequiredService(this IServiceScope scope, Type type)
    {
        return scope.GetService(type) ??
               ThrowMissingDependencyError<object>(scope);
    }
    
    /// <summary>
    /// Gets a required keyed service of type T from the service scope.
    /// Throws an exception if the service is not registered with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <param name="scope">The service scope to retrieve from</param>
    /// <param name="key">The service key for keyed service registration</param>
    /// <returns>The service instance</returns>
    /// <exception cref="ArgumentException">Thrown when the service is not registered with the specified key</exception>
    public static T GetRequiredService<T>(this IServiceScope scope, string key) where T : class
    {
        return scope.GetService(new ServiceKey(typeof(T), key)) as T ??
               ThrowMissingDependencyError<T>(key, scope);
    }

    /// <summary>
    /// Gets all services of type T from the service scope.
    /// Returns an empty collection if no services are registered.
    /// </summary>
    /// <typeparam name="T">The type of services to retrieve</typeparam>
    /// <param name="scope">The service scope to retrieve from</param>
    /// <returns>An enumerable of all registered services of type T</returns>
    /// <example>
    /// <code>
    /// using var scope = serviceProvider.CreateScope();
    /// var allServices = scope.GetServices&lt;IMyService&gt;();
    /// </code>
    /// </example>
    public static IEnumerable<T> GetServices<T>(this IServiceScope scope) where T : class =>
        GetServices<T>(scope, null);
    
    /// <summary>
    /// Gets all services of type T with the specified key from the service scope.
    /// Returns an empty collection if no services are registered with the key.
    /// </summary>
    /// <typeparam name="T">The type of services to retrieve</typeparam>
    /// <param name="scope">The service scope to retrieve from</param>
    /// <param name="key">The optional service key for keyed service registration</param>
    /// <returns>An enumerable of all registered services of type T with the specified key</returns>
    public static IEnumerable<T> GetServices<T>(this IServiceScope scope, string? key) where T : class
    {
        return scope.GetServices(new ServiceKey(typeof(T), key)).OfType<T>();
    }
    
    /// <summary>
    /// Gets an optional service of type T from the service scope.
    /// Returns null if the service is not registered.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <param name="scope">The service scope to retrieve from</param>
    /// <returns>The service instance, or null if not registered</returns>
    /// <example>
    /// <code>
    /// using var scope = serviceProvider.CreateScope();
    /// var optionalService = scope.GetOptionalService&lt;IOptionalService&gt;();
    /// if (optionalService != null)
    /// {
    ///     // Use the service
    /// }
    /// </code>
    /// </example>
    public static T? GetOptionalService<T>(this IServiceScope scope) where T : class
    {
        return scope.GetService(new ServiceKey(typeof(T))) as T;
    }
    
    /// <summary>
    /// Gets an optional keyed service of type T from the service scope.
    /// Returns null if the service is not registered with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve</typeparam>
    /// <param name="scope">The service scope to retrieve from</param>
    /// <param name="key">The service key for keyed service registration</param>
    /// <returns>The service instance, or null if not registered with the specified key</returns>
    public static T? GetOptionalService<T>(this IServiceScope scope, string key) where T : class
    {
        return scope.GetService(new ServiceKey(typeof(T), key)) as T;
    }
    
    private static T ThrowMissingDependencyError<T>(IServiceScope scope) where T : class
    {
        if (scope is ISingleton)
        {
            throw new ArgumentException(string.Format(ErrorMessageConstants.NoSingletonRegistered, typeof(T).FullName));
        }
        
        throw new ArgumentException(string.Format(ErrorMessageConstants.NoServicesRegistered, typeof(T).FullName));
    }
    
    private static T ThrowMissingDependencyError<T>(string key, IServiceScope scope) where T : class
    {
        if (scope is ISingleton)
        {
            throw new ArgumentException(string.Format(ErrorMessageConstants.NoSingletonRegisteredWithKey, typeof(T).FullName, key));
        }
        
        throw new ArgumentException(string.Format(ErrorMessageConstants.NoServicesRegisteredWithKey, typeof(T).FullName, key));
    }
}