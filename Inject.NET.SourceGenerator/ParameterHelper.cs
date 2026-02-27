using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;
using INamedTypeSymbol = Microsoft.CodeAnalysis.INamedTypeSymbol;
using ISymbol = Microsoft.CodeAnalysis.ISymbol;
using ITypeParameterSymbol = Microsoft.CodeAnalysis.ITypeParameterSymbol;
using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

namespace Inject.NET.SourceGenerator;

internal static class ParameterHelper
{
    public static IEnumerable<string> BuildParameters(INamedTypeSymbol serviceProviderType,
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies,
        ServiceModel serviceModel, Lifetime currentLifetime)
    {
        foreach (var parameter in serviceModel.Parameters)
        {
            if (WriteParameter(serviceProviderType, dependencies, parameter, serviceModel, currentLifetime) is { } written)
            {
                yield return written;
            }
        }
    }

    /// <summary>
    /// Writes the parameter resolution code for a given parameter in a service constructor.
    /// </summary>
    /// <param name="serviceProviderType">The service provider type symbol.</param>
    /// <param name="dependencies">The dictionary of all registered dependencies.</param>
    /// <param name="parameter">The parameter to resolve.</param>
    /// <param name="serviceModel">The service model containing the parameter.</param>
    /// <param name="currentLifetime">The current service lifetime context.</param>
    /// <returns>The parameter resolution code string, or null if the parameter cannot be resolved.</returns>
    public static string? WriteParameter(INamedTypeSymbol serviceProviderType,
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies,
        Parameter parameter,
        ServiceModel serviceModel,
        Lifetime currentLifetime)
    {
        // Handle Lazy<T> parameters by wrapping the inner type resolution
        if (parameter.IsLazy && parameter.LazyInnerType != null)
        {
            var innerResolution = ResolveInnerLazyType(serviceProviderType, dependencies, parameter, serviceModel, currentLifetime);
            return $"new global::System.Lazy<{parameter.LazyInnerType.GloballyQualified()}>(() => {innerResolution})";
        }

        List<ServiceModel>? models = null;

        // Handle type parameter resolution
        var typeParameterResult = HandleTypeParameter(dependencies, parameter, serviceModel);
        if (typeParameterResult.IsHandled)
        {
            models = typeParameterResult.Models;
            if (typeParameterResult.Result != null)
            {
                return typeParameterResult.Result;
            }
        }

        // Handle enumerable parameters early - extract element type and call GetServices
        if (parameter.IsEnumerable)
        {
            // Extract element type from IEnumerable<T> or IReadOnlyList<T>
            var elementType = parameter.Type is INamedTypeSymbol { IsGenericType: true } genericType
                ? genericType.TypeArguments[0]
                : parameter.Type;

            var key = parameter.Key is null ? "null" : $"\"{parameter.Key}\"";

            // Use collection literal syntax [..] to support both IEnumerable<T> and IReadOnlyList<T>
            if (serviceModel.ResolvedFromParent)
            {
                return $"[..ParentScope.GetServices<{elementType.GloballyQualified()}>({key})]";
            }

            return $"[..this.GetServices<{elementType.GloballyQualified()}>({key})]";
        }

        // Handle Func<T> parameters - wrap service resolution in a lambda
        // Since Func<T> defers resolution, we use the inner service's own lifetime
        // as the currentLifetime to bypass captive dependency checks.
        if (parameter.IsFunc && parameter.FuncInnerType != null)
        {
            var innerType = parameter.FuncInnerType;
            var innerServiceKey = new ServiceModelCollection.ServiceKey(innerType, parameter.Key);

            if (dependencies.TryGetValue(innerServiceKey, out var funcModels))
            {
                var lastModel = funcModels.Last();
                // Use the inner service's own lifetime to avoid false captive dependency errors
                var resolution = TypeHelper.GetOrConstructType(serviceProviderType, dependencies, lastModel, lastModel.Lifetime);
                return $"new global::System.Func<{innerType.GloballyQualified()}>(() => {resolution})";
            }

            // Check for open generic match
            if (innerType is INamedTypeSymbol { IsGenericType: true } innerGenericType
                && dependencies.TryGetValue(new ServiceModelCollection.ServiceKey(innerGenericType.ConstructUnboundGenericType(), parameter.Key), out funcModels))
            {
                var lastModel = funcModels.Last();
                var resolution = TypeHelper.GetOrConstructType(serviceProviderType, dependencies, lastModel, lastModel.Lifetime);
                return $"new global::System.Func<{innerType.GloballyQualified()}>(() => {resolution})";
            }

            // Fallback to runtime resolution
            if (serviceModel.ResolvedFromParent)
            {
                return $"new global::System.Func<{innerType.GloballyQualified()}>(() => ParentScope.GetRequiredService<{innerType.GloballyQualified()}>())";
            }
            return $"new global::System.Func<{innerType.GloballyQualified()}>(() => this.GetRequiredService<{innerType.GloballyQualified()}>())";
        }

        // Handle optional and nullable parameters - this matches the original logic exactly
        if (models is null && !dependencies.TryGetValue(parameter.ServiceKey, out models))
        {
            if (parameter.Type is not INamedTypeSymbol { IsGenericType: true } genericType
                || !dependencies.TryGetValue(new ServiceModelCollection.ServiceKey(genericType.ConstructUnboundGenericType(), parameter.Key), out models))
            {
                if (parameter.IsOptional)
                {
                    return $"this.GetOptionalService<{parameter.Type.GloballyQualified()}>() ?? {parameter.DefaultValue ?? "default"}";
                }

                if (parameter.IsNullable)
                {
                    return $"this.GetOptionalService<{parameter.Type.GloballyQualified()}>()";
                }

                return $"this.GetRequiredService<{parameter.Type.GloballyQualified()}>()";
            }
        }

        // At this point models should never be null
        return ResolveServiceParameter(serviceProviderType, dependencies, parameter, serviceModel, currentLifetime, models!);
    }

    /// <summary>
    /// Resolves the inner type of a Lazy&lt;T&gt; parameter to generate the factory lambda body.
    /// </summary>
    private static string ResolveInnerLazyType(
        INamedTypeSymbol serviceProviderType,
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies,
        Parameter parameter,
        ServiceModel serviceModel,
        Lifetime currentLifetime)
    {
        var innerType = parameter.LazyInnerType!;
        var innerServiceKey = parameter.ServiceKey; // Already uses LazyInnerType

        if (dependencies.TryGetValue(innerServiceKey, out var models))
        {
            var lastModel = models.Last();
            return TypeHelper.GetOrConstructType(serviceProviderType, dependencies, lastModel, currentLifetime);
        }

        // Try unbound generic lookup
        if (innerType is INamedTypeSymbol { IsGenericType: true } genericInnerType
            && dependencies.TryGetValue(new ServiceModelCollection.ServiceKey(genericInnerType.ConstructUnboundGenericType(), parameter.Key), out models))
        {
            var lastModel = models.Last();
            return TypeHelper.GetOrConstructType(serviceProviderType, dependencies, lastModel, currentLifetime);
        }

        // Fallback to runtime resolution
        if (serviceModel.ResolvedFromParent)
        {
            return $"ParentScope.GetRequiredService<{innerType.GloballyQualified()}>()";
        }

        return $"this.GetRequiredService<{innerType.GloballyQualified()}>()";
    }

    /// <summary>
    /// Handles type parameter resolution for generic service types.
    /// </summary>
    /// <param name="dependencies">The dictionary of all registered dependencies.</param>
    /// <param name="parameter">The parameter to handle.</param>
    /// <param name="serviceModel">The service model containing the parameter.</param>
    /// <returns>A result indicating if the type parameter was handled and any resolved models or result string.</returns>
    private static (bool IsHandled, List<ServiceModel>? Models, string? Result) HandleTypeParameter(
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies,
        Parameter parameter,
        ServiceModel serviceModel)
    {
        if (parameter.Type is not ITypeParameterSymbol typeParameterSymbol)
        {
            return (false, null, null);
        }

        var substitutedTypeIndex = serviceModel.ServiceType.TypeParameters.ToList()
            .FindIndex(x => x.Name == typeParameterSymbol.Name);

        if (substitutedTypeIndex == -1)
        {
            return (true, null, null);
        }

        var substitutedType = serviceModel.ServiceType.TypeArguments[substitutedTypeIndex];
        
        if (dependencies.TryGetValue(new ServiceModelCollection.ServiceKey(substitutedType, parameter.Key), out var models))
        {
            return (true, models, null);
        }

        var key = parameter.Key is null ? "null" : $"\"{parameter.Key}\"";

        if (serviceModel.ResolvedFromParent)
        {
            var result = parameter.IsOptional
                ? $"ParentScope.GetOptionalService<{substitutedType.GloballyQualified()}>({key})"
                : $"ParentScope.GetRequiredService<{substitutedType.GloballyQualified()}>({key})";
            return (true, null, result);
        }
        
        var thisResult = parameter.IsOptional
            ? $"this.GetOptionalService<{substitutedType.GloballyQualified()}>({key})"
            : $"this.GetRequiredService<{substitutedType.GloballyQualified()}>({key})";
        return (true, null, thisResult);
    }

    /// <summary>
    /// Handles optional and nullable parameter resolution when no direct dependency is found.
    /// </summary>
    /// <param name="dependencies">The dictionary of all registered dependencies.</param>
    /// <param name="parameter">The parameter to handle.</param>
    /// <returns>A result indicating if the optional parameter was handled and any resolved models or result string.</returns>
    private static (bool IsHandled, List<ServiceModel>? Models, string? Result) HandleOptionalParameter(
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies,
        Parameter parameter)
    {        
        if (dependencies.TryGetValue(parameter.ServiceKey, out var models))
        {
            return (false, models, null);
        }

        // Try to find generic type binding
        if (parameter.Type is INamedTypeSymbol { IsGenericType: true } genericType
            && dependencies.TryGetValue(new ServiceModelCollection.ServiceKey(genericType.ConstructUnboundGenericType(), parameter.Key), out models))
        {
            return (false, models, null);
        }

        // Handle optional parameters
        if (parameter.IsOptional)
        {
            var result = $"this.GetOptionalService<{parameter.Type.GloballyQualified()}>() ?? {parameter.DefaultValue ?? "default"}";
            return (true, null, result);
        }

        // Handle nullable parameters
        if (parameter.IsNullable)
        {
            var result = $"this.GetOptionalService<{parameter.Type.GloballyQualified()}>()";
            return (true, null, result);
        }

        // Fallback to required service
        var fallbackResult = $"this.GetRequiredService<{parameter.Type.GloballyQualified()}>()";
        return (true, null, fallbackResult);
    }

    /// <summary>
    /// Resolves a service parameter using the found service models.
    /// </summary>
    /// <param name="serviceProviderType">The service provider type symbol.</param>
    /// <param name="dependencies">The dictionary of all registered dependencies.</param>
    /// <param name="parameter">The parameter to resolve.</param>
    /// <param name="serviceModel">The service model containing the parameter.</param>
    /// <param name="currentLifetime">The current service lifetime context.</param>
    /// <param name="models">The service models to use for resolution.</param>
    /// <returns>The parameter resolution code string.</returns>
    private static string ResolveServiceParameter(
        INamedTypeSymbol serviceProviderType,
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies,
        Parameter parameter,
        ServiceModel serviceModel,
        Lifetime currentLifetime,
        List<ServiceModel> models)
    {
        if (parameter.IsEnumerable)
        {
            // Extract element type from IEnumerable<T> or IReadOnlyList<T>
            var elementType = parameter.Type is INamedTypeSymbol { IsGenericType: true } genericType
                ? genericType.TypeArguments[0]
                : parameter.Type;

            var key = parameter.Key is null ? "null" : $"\"{parameter.Key}\"";

            // Use collection literal syntax [..] to support both IEnumerable<T> and IReadOnlyList<T>
            if (serviceModel.ResolvedFromParent)
            {
                return $"[..ParentScope.GetServices<{elementType.GloballyQualified()}>({key})]";
            }

            return $"[..this.GetServices<{elementType.GloballyQualified()}>({key})]";
        }

        var lastModel = models.Last();

        return TypeHelper.GetOrConstructType(serviceProviderType, dependencies, lastModel, currentLifetime);
    }
}