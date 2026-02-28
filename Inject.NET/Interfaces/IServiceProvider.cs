using Inject.NET.Services;

namespace Inject.NET.Interfaces;

/// <summary>
/// Represents a service provider that creates typed service scopes and manages service lifetimes.
/// Extends the standard IServiceProvider with async disposal and typed scope creation.
/// </summary>
/// <typeparam name="TScope">The type of service scope this provider creates</typeparam>
public interface IServiceProvider<out TScope> : IAsyncDisposable, IServiceProvider
where TScope : IServiceScope
{
    /// <summary>
    /// Creates a new typed service scope for dependency resolution.
    /// </summary>
    /// <returns>A new service scope instance of type TScope</returns>
    TScope CreateTypedScope();
}

/// <summary>
/// Represents a service provider for dependency injection that extends the standard .NET IServiceProvider.
/// Provides scope creation capabilities for managing service lifetimes.
/// </summary>
public interface IServiceProvider : System.IServiceProvider
{
    /// <summary>
    /// Creates a new service scope for managing scoped and transient services.
    /// </summary>
    /// <returns>A new service scope instance</returns>
    IServiceScope CreateScope();

    /// <summary>
    /// Determines whether the specified service type is available from the provider.
    /// </summary>
    /// <param name="serviceType">The type of service to check</param>
    /// <returns>True if the service type can be resolved; otherwise, false</returns>
    bool IsService(Type serviceType);

    /// <summary>
    /// Creates a child container that inherits all registrations from this provider
    /// but can add or override registrations independently.
    /// </summary>
    /// <param name="configure">An action to configure additional or overriding registrations for the child container</param>
    /// <returns>A new child service provider</returns>
    ChildServiceProvider CreateChildContainer(Action<IServiceRegistrar> configure);
}