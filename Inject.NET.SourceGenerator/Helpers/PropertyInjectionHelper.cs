using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Helpers;

/// <summary>
/// Provides helper methods for generating property injection code.
/// Property injection sets properties marked with [Inject] on service instances after construction,
/// resolving their values from the container.
/// </summary>
internal static class PropertyInjectionHelper
{
    /// <summary>
    /// Returns whether the service model has any inject properties.
    /// </summary>
    public static bool HasInjectProperties(ServiceModel serviceModel)
    {
        return serviceModel.InjectProperties.Length > 0;
    }

    /// <summary>
    /// Returns whether the service model has any inject methods or inject properties.
    /// </summary>
    public static bool HasAnyPostConstructionInjection(ServiceModel serviceModel)
    {
        return MethodInjectionHelper.HasInjectMethods(serviceModel) || HasInjectProperties(serviceModel);
    }

    /// <summary>
    /// Generates the property injection assignment code for a factory lambda context (ServiceRegistrar).
    /// In this context, values are resolved from the 'scope' variable.
    /// </summary>
    /// <param name="serviceModel">The service model with inject properties.</param>
    /// <param name="instanceVarName">The variable name holding the constructed instance.</param>
    /// <returns>Lines of code to set inject properties, or empty if none.</returns>
    public static IEnumerable<string> GenerateFactoryPropertyAssignments(ServiceModel serviceModel, string instanceVarName)
    {
        foreach (var property in serviceModel.InjectProperties)
        {
            var resolution = BuildFactoryPropertyResolution(property);
            yield return $"{instanceVarName}.{property.PropertyName} = {resolution};";
        }
    }

    /// <summary>
    /// Generates the property injection assignment code for a scope/property context.
    /// In this context, values are resolved using 'this' or 'Singletons' references.
    /// </summary>
    /// <param name="serviceProviderType">The service provider type for resolving dependencies.</param>
    /// <param name="dependencies">All registered dependencies.</param>
    /// <param name="serviceModel">The service model with inject properties.</param>
    /// <param name="currentLifetime">Current service lifetime context.</param>
    /// <param name="instanceVarName">The variable name holding the constructed instance.</param>
    /// <returns>Lines of code to set inject properties.</returns>
    public static IEnumerable<string> GenerateScopePropertyAssignments(
        INamedTypeSymbol serviceProviderType,
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies,
        ServiceModel serviceModel,
        Lifetime currentLifetime,
        string instanceVarName)
    {
        foreach (var property in serviceModel.InjectProperties)
        {
            var resolution = BuildScopePropertyResolution(serviceProviderType, dependencies, serviceModel, currentLifetime, property);
            yield return $"{instanceVarName}.{property.PropertyName} = {resolution};";
        }
    }

    private static string BuildFactoryPropertyResolution(InjectProperty property)
    {
        // Handle Lazy<T> properties
        if (property.IsLazy && property.LazyInnerType != null)
        {
            var innerType = property.LazyInnerType;
            if (property.Key is null)
            {
                return $"new global::System.Lazy<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>())";
            }
            else
            {
                return $"new global::System.Lazy<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>(\"{property.Key}\"))";
            }
        }

        // Handle Func<T> properties
        if (property.IsFunc && property.FuncInnerType != null)
        {
            var innerType = property.FuncInnerType;
            return $"new global::System.Func<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>())";
        }

        // Handle enumerable properties
        if (property.IsEnumerable)
        {
            var elementType = property.PropertyType is INamedTypeSymbol { IsGenericType: true } genericType
                ? genericType.TypeArguments[0]
                : property.PropertyType;

            var key = property.Key is null ? "null" : $"\"{property.Key}\"";
            return $"[..scope.GetServices<{elementType.GloballyQualified()}>({key})]";
        }

        // Handle nullable (optional) properties
        if (property.IsNullable)
        {
            return $"scope.GetOptionalService<{property.PropertyType.GloballyQualified()}>()";
        }

        // Handle required properties
        return $"scope.GetRequiredService<{property.PropertyType.GloballyQualified()}>()";
    }

    private static string BuildScopePropertyResolution(
        INamedTypeSymbol serviceProviderType,
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencies,
        ServiceModel serviceModel,
        Lifetime currentLifetime,
        InjectProperty property)
    {
        // Handle Lazy<T> properties
        if (property.IsLazy && property.LazyInnerType != null)
        {
            var innerType = property.LazyInnerType;
            var innerServiceKey = new ServiceModelCollection.ServiceKey(innerType, property.Key);

            if (dependencies.TryGetValue(innerServiceKey, out var lazyModels))
            {
                var lastModel = lazyModels[^1];
                var innerResolution = TypeHelper.GetOrConstructType(serviceProviderType, dependencies, lastModel, currentLifetime);
                return $"new global::System.Lazy<{innerType.GloballyQualified()}>(() => {innerResolution})";
            }

            return $"new global::System.Lazy<{innerType.GloballyQualified()}>(() => this.GetRequiredService<{innerType.GloballyQualified()}>())";
        }

        // Handle Func<T> properties
        if (property.IsFunc && property.FuncInnerType != null)
        {
            var innerType = property.FuncInnerType;
            var innerServiceKey = new ServiceModelCollection.ServiceKey(innerType, property.Key);

            if (dependencies.TryGetValue(innerServiceKey, out var funcModels))
            {
                var lastModel = funcModels[^1];
                var resolution = TypeHelper.GetOrConstructType(serviceProviderType, dependencies, lastModel, lastModel.Lifetime);
                return $"new global::System.Func<{innerType.GloballyQualified()}>(() => {resolution})";
            }

            return $"new global::System.Func<{innerType.GloballyQualified()}>(() => this.GetRequiredService<{innerType.GloballyQualified()}>())";
        }

        // Handle enumerable properties
        if (property.IsEnumerable)
        {
            var elementType = property.PropertyType is INamedTypeSymbol { IsGenericType: true } genericType
                ? genericType.TypeArguments[0]
                : property.PropertyType;

            var key = property.Key is null ? "null" : $"\"{property.Key}\"";
            return $"[..this.GetServices<{elementType.GloballyQualified()}>({key})]";
        }

        // For non-special types, try to resolve from known dependencies
        var serviceKey = new ServiceModelCollection.ServiceKey(property.PropertyType, property.Key);

        if (dependencies.TryGetValue(serviceKey, out var models))
        {
            var lastModel = models[^1];
            return TypeHelper.GetOrConstructType(serviceProviderType, dependencies, lastModel, currentLifetime);
        }

        // Nullable (optional) properties
        if (property.IsNullable)
        {
            return $"this.GetOptionalService<{property.PropertyType.GloballyQualified()}>()";
        }

        // Required properties
        return $"this.GetRequiredService<{property.PropertyType.GloballyQualified()}>()";
    }
}
