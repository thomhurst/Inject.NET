using System.Reflection;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

/// <summary>
/// A DispatchProxy-based interceptor proxy that wraps a service instance with one or more interceptors.
/// All method calls on the proxy are routed through the interceptor chain before reaching the target service.
/// </summary>
/// <typeparam name="TService">The service interface type to proxy. Must be an interface.</typeparam>
/// <remarks>
/// This proxy uses <see cref="System.Reflection.DispatchProxy"/> which is built into .NET.
/// It creates a runtime proxy implementing <typeparamref name="TService"/> that intercepts all method calls.
/// </remarks>
public class InterceptorProxy<TService> : DispatchProxy where TService : class
{
    private TService _target = null!;
    private IReadOnlyList<IInterceptor> _interceptors = Array.Empty<IInterceptor>();

    /// <summary>
    /// Creates a new proxy instance that wraps the target service with the specified interceptors.
    /// </summary>
    /// <param name="target">The actual service instance to proxy</param>
    /// <param name="interceptors">The interceptors to apply to method calls</param>
    /// <returns>A proxy instance implementing <typeparamref name="TService"/></returns>
    /// <exception cref="ArgumentNullException">Thrown if target or interceptors is null</exception>
    public static TService Create(TService target, IReadOnlyList<IInterceptor> interceptors)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(interceptors);

        // DispatchProxy.Create returns an object that implements TService and extends InterceptorProxy<TService>
        var proxy = Create<TService, InterceptorProxy<TService>>();
        var interceptorProxy = (InterceptorProxy<TService>)(object)proxy;

        interceptorProxy._target = target;
        interceptorProxy._interceptors = interceptors;

        return proxy;
    }

    /// <summary>
    /// Invoked by the runtime for every method call on the proxy.
    /// Routes the call through the interceptor chain.
    /// </summary>
    /// <param name="targetMethod">The method being called</param>
    /// <param name="args">The arguments passed to the method</param>
    /// <returns>The return value from the interceptor chain or the actual method</returns>
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null)
        {
            throw new ArgumentNullException(nameof(targetMethod));
        }

        var arguments = args ?? Array.Empty<object?>();

        if (_interceptors.Count == 0)
        {
            // No interceptors - call the target directly
            return targetMethod.Invoke(_target, arguments);
        }

        var invocation = new Invocation(_target, targetMethod, arguments, _interceptors);

        // Start the interceptor chain
        var task = invocation.ProceedAsync();

        // Determine the return type and handle accordingly
        var returnType = targetMethod.ReturnType;

        if (returnType == typeof(void))
        {
            // Fire and forget for void methods - wait synchronously
            task.GetAwaiter().GetResult();
            return null;
        }

        if (returnType == typeof(Task))
        {
            // Return the task directly for async void-like methods
            return ConvertToTask(task);
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            // Return Task<T> by converting the result
            var resultType = returnType.GetGenericArguments()[0];
            return ConvertToGenericTask(task, resultType);
        }

        if (returnType == typeof(ValueTask))
        {
            return new ValueTask(ConvertToTask(task));
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var genericTask = ConvertToGenericTask(task, resultType);
            // Create ValueTask<T> from Task<T>
            var valueTaskType = typeof(ValueTask<>).MakeGenericType(resultType);
            return Activator.CreateInstance(valueTaskType, genericTask);
        }

        // Synchronous method - wait for result
        return task.GetAwaiter().GetResult();
    }

    private static async Task ConvertToTask(Task<object?> task)
    {
        await task.ConfigureAwait(false);
    }

    private static object ConvertToGenericTask(Task<object?> task, Type resultType)
    {
        var method = typeof(InterceptorProxy<TService>)
            .GetMethod(nameof(ConvertToGenericTaskHelper), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(resultType);

        return method.Invoke(null, [task])!;
    }

    private static async Task<T> ConvertToGenericTaskHelper<T>(Task<object?> task)
    {
        var result = await task.ConfigureAwait(false);
        return (T)result!;
    }
}
