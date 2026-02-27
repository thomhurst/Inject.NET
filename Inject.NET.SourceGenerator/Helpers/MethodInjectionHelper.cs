using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Helpers;

/// <summary>
/// Provides helper methods for generating method injection code.
/// Method injection calls methods marked with [Inject] on service instances after construction,
/// resolving their parameters from the container.
/// </summary>
internal static class MethodInjectionHelper
{
    /// <summary>
    /// Returns whether the service model has any inject methods.
    /// </summary>
    public static bool HasInjectMethods(ServiceModel serviceModel)
    {
        return serviceModel.InjectMethods.Length > 0;
    }

    /// <summary>
    /// Returns whether any inject methods on the service model are async.
    /// </summary>
    public static bool HasAsyncInjectMethods(ServiceModel serviceModel)
    {
        foreach (var method in serviceModel.InjectMethods)
        {
            if (method.IsAsync)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Generates the method injection call code for a factory lambda context (ServiceRegistrar).
    /// In this context, parameters are resolved from the 'scope' variable.
    /// Returns null if there are no inject methods.
    /// </summary>
    /// <param name="serviceModel">The service model with inject methods.</param>
    /// <param name="instanceVarName">The variable name holding the constructed instance.</param>
    /// <returns>Lines of code to invoke inject methods, or empty if none.</returns>
    public static IEnumerable<string> GenerateFactoryInjectCalls(ServiceModel serviceModel, string instanceVarName)
    {
        foreach (var method in serviceModel.InjectMethods)
        {
            var parameters = BuildFactoryParameters(method);
            var call = $"{instanceVarName}.{method.MethodName}({string.Join(", ", parameters)})";

            if (method.IsAsync)
            {
                // For async methods in factory lambdas, we need to use .GetAwaiter().GetResult()
                // since the factory delegate returns object, not Task<object>
                yield return $"{call}.GetAwaiter().GetResult();";
            }
            else
            {
                yield return $"{call};";
            }
        }
    }

    /// <summary>
    /// Generates the method injection call code for a scope/property context.
    /// In this context, parameters are resolved using 'this' or 'Singletons' references.
    /// </summary>
    /// <param name="serviceProviderType">The service provider type for resolving dependencies.</param>
    /// <param name="dependencies">All registered dependencies.</param>
    /// <param name="serviceModel">The service model with inject methods.</param>
    /// <param name="currentLifetime">Current service lifetime context.</param>
    /// <param name="instanceVarName">The variable name holding the constructed instance.</param>
    /// <returns>Lines of code to invoke inject methods.</returns>
    public static IEnumerable<string> GenerateScopeInjectCalls(
        INamedTypeSymbol serviceProviderType,
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies,
        ServiceModel serviceModel,
        Lifetime currentLifetime,
        string instanceVarName)
    {
        foreach (var method in serviceModel.InjectMethods)
        {
            var parameters = BuildScopeParameters(serviceProviderType, dependencies, serviceModel, currentLifetime, method);
            var call = $"{instanceVarName}.{method.MethodName}({string.Join(", ", parameters)})";

            if (method.IsAsync)
            {
                yield return $"{call}.GetAwaiter().GetResult();";
            }
            else
            {
                yield return $"{call};";
            }
        }
    }

    private static IEnumerable<string> BuildFactoryParameters(InjectMethod method)
    {
        foreach (var parameter in method.Parameters)
        {
            // Handle Lazy<T> parameters
            if (parameter.IsLazy && parameter.LazyInnerType != null)
            {
                var innerType = parameter.LazyInnerType;
                if (parameter.Key is null)
                {
                    yield return $"new global::System.Lazy<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>())";
                }
                else
                {
                    yield return $"new global::System.Lazy<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>(\"{parameter.Key}\"))";
                }
            }
            // Handle Func<T> parameters
            else if (parameter.IsFunc && parameter.FuncInnerType != null)
            {
                var innerType = parameter.FuncInnerType;
                yield return $"new global::System.Func<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>())";
            }
            // Handle enumerable parameters
            else if (parameter.IsEnumerable)
            {
                var elementType = parameter.Type is INamedTypeSymbol { IsGenericType: true } genericType
                    ? genericType.TypeArguments[0]
                    : parameter.Type;

                var key = parameter.Key is null ? "null" : $"\"{parameter.Key}\"";
                yield return $"[..scope.GetServices<{elementType.GloballyQualified()}>({key})]";
            }
            // Handle optional parameters
            else if (parameter.IsOptional)
            {
                yield return $"scope.GetOptionalService<{parameter.Type.GloballyQualified()}>() ?? {parameter.DefaultValue ?? "default"}";
            }
            // Handle nullable parameters
            else if (parameter.IsNullable)
            {
                yield return $"scope.GetOptionalService<{parameter.Type.GloballyQualified()}>()";
            }
            // Handle required parameters
            else
            {
                yield return $"scope.GetRequiredService<{parameter.Type.GloballyQualified()}>()";
            }
        }
    }

    private static IEnumerable<string> BuildScopeParameters(
        INamedTypeSymbol serviceProviderType,
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies,
        ServiceModel serviceModel,
        Lifetime currentLifetime,
        InjectMethod method)
    {
        foreach (var parameter in method.Parameters)
        {
            if (ParameterHelper.WriteParameter(serviceProviderType, dependencies, parameter, serviceModel, currentLifetime) is { } written)
            {
                yield return written;
            }
        }
    }
}
