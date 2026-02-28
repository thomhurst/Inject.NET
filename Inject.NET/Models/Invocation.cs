using System.Reflection;
using Inject.NET.Interfaces;

namespace Inject.NET.Models;

/// <summary>
/// Default implementation of <see cref="IInvocation"/> that manages the interceptor chain
/// and delegates to the actual service method when all interceptors have been processed.
/// </summary>
public class Invocation : IInvocation
{
    private readonly IReadOnlyList<IInterceptor> _interceptors;
    private int _currentIndex;

    /// <summary>
    /// Initializes a new instance of <see cref="Invocation"/>.
    /// </summary>
    /// <param name="target">The target service instance</param>
    /// <param name="method">The method being invoked</param>
    /// <param name="arguments">The arguments passed to the method</param>
    /// <param name="interceptors">The chain of interceptors to process</param>
    public Invocation(object target, MethodInfo method, object?[] arguments, IReadOnlyList<IInterceptor> interceptors)
    {
        Target = target;
        Method = method;
        Arguments = arguments;
        _interceptors = interceptors;
        _currentIndex = 0;
    }

    /// <inheritdoc />
    public MethodInfo Method { get; }

    /// <inheritdoc />
    public object?[] Arguments { get; }

    /// <inheritdoc />
    public object? ReturnValue { get; set; }

    /// <inheritdoc />
    public object Target { get; }

    /// <inheritdoc />
    public async Task<object?> ProceedAsync()
    {
        if (_currentIndex < _interceptors.Count)
        {
            var interceptor = _interceptors[_currentIndex];
            _currentIndex++;
            ReturnValue = await interceptor.InterceptAsync(this);
            return ReturnValue;
        }

        // All interceptors processed - invoke the actual method on the target
        object? result;
        try
        {
            result = Method.Invoke(Target, Arguments);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw; // Unreachable, but required for compiler
        }

        // Handle async methods that return Task or Task<T>
        if (result is Task task)
        {
            await task.ConfigureAwait(false);

            // Extract the result from Task<T>
            var taskType = task.GetType();
            if (taskType.IsGenericType)
            {
                var resultProperty = taskType.GetProperty("Result");
                ReturnValue = resultProperty?.GetValue(task);
            }
            else
            {
                ReturnValue = null;
            }
        }
        else if (result is ValueTask valueTask)
        {
            await valueTask.ConfigureAwait(false);
            ReturnValue = null;
        }
        else
        {
            // Check for ValueTask<T> which is a struct and won't match the Task check
            var resultType = result?.GetType();
            if (resultType is { IsGenericType: true } && resultType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                // Convert ValueTask<T> to Task<T> to await it
                var asTaskMethod = resultType.GetMethod("AsTask")!;
                var innerTask = (Task)asTaskMethod.Invoke(result, null)!;
                await innerTask.ConfigureAwait(false);

                var resultProperty = innerTask.GetType().GetProperty("Result");
                ReturnValue = resultProperty?.GetValue(innerTask);
            }
            else
            {
                ReturnValue = result;
            }
        }

        return ReturnValue;
    }
}
