using System.Reflection;

namespace Inject.NET.Interfaces;

/// <summary>
/// Represents an intercepted method invocation, providing access to method metadata,
/// arguments, and the ability to proceed to the next interceptor or actual implementation.
/// </summary>
public interface IInvocation
{
    /// <summary>
    /// Gets the <see cref="MethodInfo"/> of the method being invoked.
    /// </summary>
    MethodInfo Method { get; }

    /// <summary>
    /// Gets the arguments passed to the method being invoked.
    /// </summary>
    object?[] Arguments { get; }

    /// <summary>
    /// Gets or sets the return value of the method invocation.
    /// This value is set after <see cref="ProceedAsync"/> completes and can be modified by interceptors.
    /// </summary>
    object? ReturnValue { get; set; }

    /// <summary>
    /// Gets the target service instance that the method will ultimately be invoked on.
    /// </summary>
    object Target { get; }

    /// <summary>
    /// Proceeds to the next interceptor in the chain or invokes the actual method on the target service.
    /// </summary>
    /// <returns>
    /// A task representing the asynchronous operation, with the method's return value (or null for void methods).
    /// </returns>
    Task<object?> ProceedAsync();
}
