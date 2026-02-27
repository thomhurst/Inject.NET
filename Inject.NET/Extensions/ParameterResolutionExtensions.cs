using System.Reflection;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Extensions;

/// <summary>
/// Provides extension methods for resolving services with parameter overrides.
/// Allows passing runtime values for constructor parameters that would normally
/// be resolved from the container.
/// </summary>
public static class ParameterResolutionExtensions
{
    /// <summary>
    /// Resolves a service of type T, overriding specific constructor parameters
    /// with the provided values. Non-overridden parameters are resolved from
    /// the container as usual.
    /// </summary>
    /// <typeparam name="T">The type of service to resolve</typeparam>
    /// <param name="scope">The service scope to resolve dependencies from</param>
    /// <param name="parameters">The parameter overrides to apply</param>
    /// <returns>A new instance of T with the specified parameter overrides applied</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when type T has no public constructors.
    /// </exception>
    /// <example>
    /// <code>
    /// var repo = scope.Resolve&lt;Repository&gt;(new NamedParameter("connectionString", "Server=..."));
    /// var handler = scope.Resolve&lt;Handler&gt;(new TypedParameter&lt;string&gt;("customValue"));
    /// var service = scope.Resolve&lt;MyService&gt;(
    ///     new TypedParameter&lt;int&gt;(42),
    ///     new NamedParameter("name", "test")
    /// );
    /// </code>
    /// </example>
    public static T Resolve<T>(this IServiceScope scope, params Parameter[] parameters) where T : class
    {
        return (T)ResolveInternal(scope, typeof(T), parameters);
    }

    /// <summary>
    /// Resolves a service of the specified type, overriding specific constructor parameters
    /// with the provided values. Non-overridden parameters are resolved from
    /// the container as usual.
    /// </summary>
    /// <param name="scope">The service scope to resolve dependencies from</param>
    /// <param name="serviceType">The type of service to resolve</param>
    /// <param name="parameters">The parameter overrides to apply</param>
    /// <returns>A new instance of the specified type with the specified parameter overrides applied</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the type has no public constructors.
    /// </exception>
    public static object Resolve(this IServiceScope scope, Type serviceType, params Parameter[] parameters)
    {
        return ResolveInternal(scope, serviceType, parameters);
    }

    private static object ResolveInternal(IServiceScope scope, Type type, Parameter[] parameters)
    {
        // Find best constructor - prefer constructor with most parameters (same logic as ServiceFactory<T>)
        var constructor = type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        if (constructor == null)
        {
            throw new InvalidOperationException(
                $"Cannot create instance of type '{type.FullName}'. " +
                $"The type has no public constructors. " +
                $"Ensure the type has at least one public constructor for dependency injection.");
        }

        var constructorParams = constructor.GetParameters();
        var arguments = new object?[constructorParams.Length];

        for (int i = 0; i < constructorParams.Length; i++)
        {
            var param = constructorParams[i];
            var paramType = param.ParameterType;
            var paramName = param.Name ?? string.Empty;

            // Check if any parameter override matches
            if (TryMatchParameter(parameters, paramType, paramName, out var overrideValue))
            {
                arguments[i] = overrideValue;
                continue;
            }

            // No override matched - resolve from the container
            var serviceKey = new ServiceKey(paramType);

            bool isNullableReference = !paramType.IsValueType;
            bool isNullableValueType = Nullable.GetUnderlyingType(paramType) != null;
            bool hasDefaultValue = param.HasDefaultValue;
            bool isOptional = isNullableReference || isNullableValueType || hasDefaultValue;

            var resolved = scope.GetService(serviceKey);

            if (resolved != null)
            {
                arguments[i] = resolved;
            }
            else if (hasDefaultValue)
            {
                arguments[i] = param.DefaultValue;
            }
            else if (isOptional)
            {
                arguments[i] = null;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot resolve parameter '{paramName}' of type '{paramType.FullName}' " +
                    $"for service '{type.FullName}'. " +
                    $"No parameter override was provided and the type is not registered in the container.");
            }
        }

        return constructor.Invoke(arguments);
    }

    private static bool TryMatchParameter(Parameter[] parameters, Type paramType, string paramName, out object? value)
    {
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].TryMatch(paramType, paramName, out value))
            {
                return true;
            }
        }

        value = null;
        return false;
    }
}
