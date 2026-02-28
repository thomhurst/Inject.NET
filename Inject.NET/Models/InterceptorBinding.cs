using Inject.NET.Interfaces;

namespace Inject.NET.Models;

/// <summary>
/// Represents a binding between a service type and an interceptor.
/// Used to track which interceptors apply to which service types during registration.
/// </summary>
public class InterceptorBinding
{
    /// <summary>
    /// Gets the service type that this interceptor should be applied to.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the interceptor implementation type, if registered by type.
    /// </summary>
    public Type? InterceptorType { get; }

    /// <summary>
    /// Gets the factory function for creating the interceptor, if registered by factory.
    /// </summary>
    public Func<IServiceScope, IInterceptor>? InterceptorFactory { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="InterceptorBinding"/> with a type-based interceptor.
    /// </summary>
    /// <param name="serviceType">The service type to intercept</param>
    /// <param name="interceptorType">The interceptor implementation type</param>
    public InterceptorBinding(Type serviceType, Type? interceptorType = null)
    {
        ServiceType = serviceType;
        InterceptorType = interceptorType;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="InterceptorBinding"/> with a factory-based interceptor.
    /// </summary>
    /// <param name="serviceType">The service type to intercept</param>
    /// <param name="interceptorFactory">The factory function for creating the interceptor</param>
    public InterceptorBinding(Type serviceType, Func<IServiceScope, IInterceptor> interceptorFactory)
    {
        ServiceType = serviceType;
        InterceptorFactory = interceptorFactory;
    }
}
