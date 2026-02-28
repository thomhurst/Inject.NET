namespace Inject.NET.Interfaces;

/// <summary>
/// Defines an interceptor that can wrap service method calls with cross-cutting concerns
/// such as logging, caching, retry logic, or authorization.
/// </summary>
/// <remarks>
/// Interceptors are applied to interface-based services using DispatchProxy and are invoked
/// for every method call on the proxied service. Multiple interceptors can be chained together,
/// with each interceptor calling <see cref="IInvocation.ProceedAsync"/> to invoke the next
/// interceptor in the chain or the actual service method.
/// </remarks>
/// <example>
/// <code>
/// public class LoggingInterceptor : IInterceptor
/// {
///     public async Task&lt;object?&gt; InterceptAsync(IInvocation invocation)
///     {
///         Console.WriteLine($"Calling {invocation.Method.Name}");
///         var result = await invocation.ProceedAsync();
///         Console.WriteLine($"Completed {invocation.Method.Name}");
///         return result;
///     }
/// }
/// </code>
/// </example>
public interface IInterceptor
{
    /// <summary>
    /// Intercepts a method invocation on a proxied service.
    /// </summary>
    /// <param name="invocation">
    /// The invocation context containing method information, arguments, and the ability to proceed to the next
    /// interceptor or the actual service method.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation, with the method's return value (or null for void methods).
    /// </returns>
    Task<object?> InterceptAsync(IInvocation invocation);
}
