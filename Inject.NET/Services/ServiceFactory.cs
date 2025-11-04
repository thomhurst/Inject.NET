using System.Linq.Expressions;
using System.Reflection;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;

namespace Inject.NET.Services;

/// <summary>
/// High-performance generic service factory that compiles and caches constructor expressions.
/// Uses generic static fields to ensure one factory per type T with zero dictionary lookups.
/// Thread-safe and optimized for minimal overhead after first resolution.
/// </summary>
/// <typeparam name="T">The service type to create</typeparam>
internal static class ServiceFactory<T> where T : class
{
    /// <summary>
    /// Generic static field - exists once per type T in the entire application.
    /// Initialized on first access, then cached forever with no synchronization overhead.
    /// </summary>
    private static readonly Func<IServiceScope, object> _cachedFactory = CompileFactory();

    /// <summary>
    /// Creates an instance of type T using the cached compiled factory.
    /// First call per type: ~50μs (expression compilation)
    /// Subsequent calls: ~2ns (cached delegate invocation)
    /// </summary>
    /// <param name="scope">The service scope to resolve dependencies from</param>
    /// <param name="type">The service type (unused for non-generic services, included for signature compatibility)</param>
    /// <param name="key">The service key (unused for non-keyed services, included for signature compatibility)</param>
    /// <returns>A new instance of type T with all dependencies resolved</returns>
    public static object Create(IServiceScope scope, Type type, string? key)
    {
        return _cachedFactory(scope);
    }

    /// <summary>
    /// Compiles an expression tree that creates instances of type T by resolving constructor dependencies.
    /// This method runs once per type T when the generic static field is initialized.
    /// </summary>
    /// <returns>A compiled delegate that creates instances of T</returns>
    /// <exception cref="InvalidOperationException">Thrown if type T has no public constructors</exception>
    private static Func<IServiceScope, object> CompileFactory()
    {
        var scopeParam = Expression.Parameter(typeof(IServiceScope), "scope");
        var typeofT = typeof(T);

        // Find best constructor - prefer constructor with most parameters for maximum dependency injection
        var constructor = typeofT
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();

        if (constructor == null)
        {
            throw new InvalidOperationException(
                $"Cannot create instance of type '{typeofT.FullName}'. " +
                $"The type has no public constructors. " +
                $"Ensure the type has at least one public constructor for dependency injection.");
        }

        var constructorParams = constructor.GetParameters();
        var parameterExpressions = new Expression[constructorParams.Length];

        // Build expression to resolve each constructor parameter from the service scope
        for (int i = 0; i < constructorParams.Length; i++)
        {
            var param = constructorParams[i];
            var paramType = param.ParameterType;

            // Check if parameter is optional (nullable reference, nullable value type, or has default value)
            bool isNullableReference = !paramType.IsValueType;
            bool isNullableValueType = Nullable.GetUnderlyingType(paramType) != null;
            bool hasDefaultValue = param.HasDefaultValue;
            bool isOptional = isNullableReference || isNullableValueType || hasDefaultValue;

            // Get the actual type to resolve (unwrap nullable value types)
            var typeToResolve = isNullableValueType
                ? Nullable.GetUnderlyingType(paramType)!
                : paramType;

            // Choose appropriate service resolution method
            // Optional parameters use GetOptionalService, required parameters use GetRequiredService
            var methodName = isOptional
                ? nameof(ServiceScopeExtensions.GetOptionalService)
                : nameof(ServiceScopeExtensions.GetRequiredService);

            var getServiceMethod = typeof(ServiceScopeExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == methodName && m.GetParameters().Length == 1);

            var genericMethod = getServiceMethod.MakeGenericMethod(typeToResolve);

            // Generate call: scope.GetRequiredService<TParam>() or scope.GetOptionalService<TParam>()
            var callExpression = Expression.Call(genericMethod, scopeParam);

            // Handle conversion and default values
            Expression finalExpression = callExpression;

            // If parameter has a default value and the service might be null, use coalesce to provide default
            if (hasDefaultValue && isOptional)
            {
                var defaultValueExpression = Expression.Constant(param.DefaultValue, paramType);
                finalExpression = Expression.Coalesce(
                    Expression.Convert(callExpression, paramType),
                    defaultValueExpression);
            }
            else if (callExpression.Type != paramType)
            {
                // Type conversion if needed (e.g., for nullable value types)
                finalExpression = Expression.Convert(callExpression, paramType);
            }

            parameterExpressions[i] = finalExpression;
        }

        // Build constructor call: new T(param1, param2, param3, ...)
        var newExpression = Expression.New(constructor, parameterExpressions);

        // Convert to object for the return type
        var convertExpression = Expression.Convert(newExpression, typeof(object));

        // Compile the complete expression: scope => (object)new T(scope.GetService<P1>(), ...)
        var lambda = Expression.Lambda<Func<IServiceScope, object>>(
            convertExpression,
            scopeParam);

        return lambda.Compile();
    }
}
