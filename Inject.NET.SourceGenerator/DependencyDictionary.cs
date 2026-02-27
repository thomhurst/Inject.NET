using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class DependencyDictionary
{
    /// <summary>
    /// Creates a dictionary of service dependencies from the provided attribute data.
    /// </summary>
    /// <param name="compilation">The compilation context for type resolution.</param>
    /// <param name="dependencyAttributes">Array of dependency attributes to process.</param>
    /// <param name="tenantName">Optional tenant name for multi-tenant scenarios.</param>
    /// <returns>A dictionary mapping service keys to lists of service models.</returns>
    public static IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> Create(Compilation compilation,
        AttributeData[] dependencyAttributes, string? tenantName)
    {
        var serviceBuilders = new List<ServiceModelBuilder>();
        
        ProcessAttributeData(compilation, dependencyAttributes, tenantName, serviceBuilders);
        ProcessParameters(serviceBuilders);
        
        return BuildServiceDictionary(serviceBuilders);
    }

    /// <summary>
    /// Processes dependency attributes and creates service model builders for each service.
    /// Handles both regular services and generic type definitions with their constructed variants.
    /// </summary>
    /// <param name="compilation">The compilation context for type resolution.</param>
    /// <param name="dependencyAttributes">Array of dependency attributes to process.</param>
    /// <param name="tenantName">Optional tenant name for multi-tenant scenarios.</param>
    /// <param name="serviceBuilders">List to populate with service model builders.</param>
    private static void ProcessAttributeData(Compilation compilation, AttributeData[] dependencyAttributes, 
        string? tenantName, List<ServiceModelBuilder> serviceBuilders)
    {
        // Sort dependency attributes by constructor argument length
        Array.Sort(dependencyAttributes, (x, y) => x.ConstructorArguments.Length.CompareTo(y.ConstructorArguments.Length));
        
        foreach (var attributeData in dependencyAttributes)
        {
            var attributeClass = attributeData.AttributeClass;

            if (attributeClass is null)
            {
                continue;
            }
            
            if (!TryGetServiceAndImplementation(attributeData, out var serviceType, out var implementationType)
                || serviceType is null
                || implementationType is null)
            {
                continue;
            }

            string? key = null;
            foreach (var namedArg in attributeData.NamedArguments)
            {
                if (namedArg.Key == "Key")
                {
                    key = namedArg.Value.Value as string;
                    break;
                }
            }
            var lifetime = EnumPolyfill.Parse<Lifetime>(attributeData.AttributeClass!.Name.Replace("Attribute", string.Empty));
            var isGenericDefinition = serviceType.IsGenericDefinition();
            
            Add(compilation, serviceType, implementationType, serviceBuilders, key, lifetime, tenantName);

            if (isGenericDefinition)
            {
                ProcessGenericTypes(compilation, serviceType, implementationType, serviceBuilders, key, lifetime, tenantName);
            }
        }
    }

    /// <summary>
    /// Processes generic type definitions by creating constructed variants for all type argument combinations.
    /// Ensures that specific generic types are available for dependency injection when needed.
    /// </summary>
    /// <param name="compilation">The compilation context for type resolution.</param>
    /// <param name="serviceType">The generic service type definition.</param>
    /// <param name="implementationType">The generic implementation type definition.</param>
    /// <param name="serviceBuilders">List of service model builders to add constructed types to.</param>
    /// <param name="key">Optional service key for keyed services.</param>
    /// <param name="lifetime">Service lifetime scope.</param>
    /// <param name="tenantName">Optional tenant name for multi-tenant scenarios.</param>
    private static void ProcessGenericTypes(Compilation compilation, INamedTypeSymbol serviceType, 
        INamedTypeSymbol implementationType, List<ServiceModelBuilder> serviceBuilders, 
        string? key, Lifetime lifetime, string? tenantName)
    {
        var constructedTypes = GenericTypeHelper.GetConstructedTypes(compilation, serviceType);

        foreach (var constructedType in constructedTypes)
        {
            bool found = false;
            foreach (var builder in serviceBuilders)
            {
                if (SymbolEqualityComparer.Default.Equals(builder.ServiceType, constructedType))
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                Add(compilation,
                    constructedType,
                    implementationType.IsGenericType
                        ? implementationType.OriginalDefinition.Construct([..constructedType.TypeArguments])
                        : implementationType,
                    serviceBuilders,
                    key,
                    lifetime, 
                    tenantName);
            }
        }
    }

    /// <summary>
    /// Processes constructor parameters to ensure all dependencies are registered.
    /// Creates additional service registrations for generic parameters that require specific type arguments.
    /// </summary>
    /// <param name="serviceBuilders">List of service model builders to analyze and extend.</param>
    private static void ProcessParameters(List<ServiceModelBuilder> serviceBuilders)
    {
        // TODO Merge with tenant ones too
        var parametersToProcess = new List<Parameter>();
        foreach (var builder in serviceBuilders)
        {
            foreach (var parameter in builder.Parameters)
            {
                parametersToProcess.Add(parameter);
            }
        }
        
        foreach (var parameter in parametersToProcess)
        {
            bool serviceExists = false;
            foreach (var builder in serviceBuilders)
            {
                if (SymbolEqualityComparer.Default.Equals(builder.ServiceType, parameter.Type))
                {
                    serviceExists = true;
                    break;
                }
            }
            
            if (serviceExists)
            {
                continue;
            }

            if (parameter.Type is not INamedTypeSymbol { IsGenericType: true } namedParameterType)
            {
                continue;
            }
                
            ServiceModelBuilder? found = null;
            var unboundGenericType = namedParameterType.ConstructUnboundGenericType();
            foreach (var builder in serviceBuilders)
            {
                if (SymbolEqualityComparer.Default.Equals(builder.ServiceType, unboundGenericType))
                {
                    found = builder;
                    break;
                }
            }
            
            if (found is not null)
            {
                var implementationType = found.ImplementationType.OriginalDefinition.Construct([..namedParameterType.TypeArguments]);

                if (!namedParameterType.IsGenericDefinition()
                    && !implementationType.IsGenericDefinition())
                {
                    serviceBuilders.Add(found with
                    {
                        ServiceType = namedParameterType,
                        ImplementationType = implementationType,
                        IsOpenGeneric = false,
                    });
                }
            }
        }
    }

    /// <summary>
    /// Builds the final service dictionary from service model builders.
    /// Groups services by their service key and converts builders to service models with proper indexing.
    /// </summary>
    /// <param name="serviceBuilders">List of service model builders to convert.</param>
    /// <returns>A dictionary mapping service keys to lists of service models.</returns>
    private static IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> BuildServiceDictionary(
        List<ServiceModelBuilder> serviceBuilders)
    {
        // Group service builders by service key
        var builderGroups = new Dictionary<ServiceModelCollection.ServiceKey, List<ServiceModelBuilder>>();
        foreach (var builder in serviceBuilders)
        {
            var serviceKey = new ServiceModelCollection.ServiceKey(builder.ServiceType, builder.Key);
            if (!builderGroups.TryGetValue(serviceKey, out var group))
            {
                group = new List<ServiceModelBuilder>();
                builderGroups[serviceKey] = group;
            }
            group.Add(builder);
        }

        // Convert to final dictionary
        var result = new Dictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>>();
        foreach (var kvp in builderGroups)
        {
            var serviceModels = new List<ServiceModel>(kvp.Value.Count);
            for (int index = 0; index < kvp.Value.Count; index++)
            {
                var smb = kvp.Value[index];
                var parameters = new Parameter[smb.Parameters.Length];
                for (int i = 0; i < smb.Parameters.Length; i++)
                {
                    parameters[i] = smb.Parameters[i];
                }
                
                serviceModels.Add(new ServiceModel
                {
                    ServiceType = smb.ServiceType,
                    ImplementationType = smb.ImplementationType,
                    ResolvedFromParent = false,
                    Key = smb.Key,
                    Lifetime = smb.Lifetime,
                    IsOpenGeneric = smb.IsOpenGeneric,
                    Parameters = parameters,
                    Index = index,
                    TenantName = smb.TenantName
                });
            }
            result[kvp.Key] = serviceModels;
        }
        
        return result;
    }

    private static void Add(Compilation compilation, INamedTypeSymbol serviceType, INamedTypeSymbol implementationType,
        List<ServiceModelBuilder> list, string? key, Lifetime lifetime, string? tenantName)
    {
        var isGenericDefinition = serviceType.IsGenericDefinition();

        var parameters = GetParameters(implementationType, compilation);

        list.Add(new ServiceModelBuilder
        {
            ServiceType = serviceType,
            ImplementationType = implementationType,
            IsOpenGeneric = isGenericDefinition,
            Parameters = parameters,
            Key = key,
            Lifetime = lifetime,
            TenantName = tenantName
        });
    }

    private static bool TryGetServiceAndImplementation(AttributeData attributeData,
        out INamedTypeSymbol? serviceType, out INamedTypeSymbol? implementationType)
    {
        var attributeClass = attributeData.AttributeClass!;
        
        if (attributeClass.TypeArguments.Length == 0 && attributeData.ConstructorArguments.Length == 1)
        {
            serviceType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
            implementationType = serviceType;
            return true;
        }

        if (attributeClass.TypeArguments.Length == 0 && attributeData.ConstructorArguments.Length == 2)
        {
            serviceType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
            implementationType = attributeData.ConstructorArguments[1].Value as INamedTypeSymbol;
            return true;
        }

        if (attributeClass.TypeArguments.Length == 1)
        {
            serviceType = attributeClass.TypeArguments[0] as INamedTypeSymbol;
            implementationType = serviceType;
            return true;
        }

        if (attributeClass.TypeArguments.Length == 2)
        {
            serviceType = attributeClass.TypeArguments[0] as INamedTypeSymbol;
            implementationType = attributeClass.TypeArguments[1] as INamedTypeSymbol;
            return true;
        }

        serviceType = null;
        implementationType = null;
        return false;
    }

    private static Parameter[] GetParameters(ITypeSymbol type, Compilation compilation)
    {
        var namedTypeSymbol = type as INamedTypeSymbol;

        if (namedTypeSymbol?.IsUnboundGenericType is true)
        {
            namedTypeSymbol = namedTypeSymbol.OriginalDefinition;
        }
        
        ImmutableArray<IParameterSymbol> parameters = default;
        if (namedTypeSymbol?.InstanceConstructors != null)
        {
            foreach (var constructor in namedTypeSymbol.InstanceConstructors)
            {
                if (!constructor.IsImplicitlyDeclared)
                {
                    parameters = constructor.Parameters;
                    break;
                }
            }
        }

        if (parameters.IsDefaultOrEmpty)
        {
            return [];
        }
        
        var result = new Parameter[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            result[i] = Map(parameters[i], compilation);
        }
        return result;
    }

    private static Parameter Map(IParameterSymbol parameterSymbol, Compilation compilation)
    {
        var isLazy = CheckIsLazy(parameterSymbol.Type, out var lazyInnerType);
        var isFunc = CheckIsFunc(parameterSymbol.Type, out var funcInnerType);

        return new Parameter
        {
            Type = parameterSymbol.Type,
            DefaultValue = !parameterSymbol.HasExplicitDefaultValue ? null : parameterSymbol.ExplicitDefaultValue,
            IsEnumerable = CheckIsEnumerable(parameterSymbol.Type, compilation),
            IsOptional = parameterSymbol.IsOptional,
            IsNullable = parameterSymbol.NullableAnnotation == NullableAnnotation.Annotated,
            IsLazy = isLazy,
            LazyInnerType = lazyInnerType,
            IsFunc = isFunc,
            FuncInnerType = funcInnerType,
            Key = GetServiceKeyFromAttributes(parameterSymbol, compilation)
        };
    }

    private static bool CheckIsFunc(ITypeSymbol parameterType, out ITypeSymbol? innerType)
    {
        innerType = null;

        if (parameterType is INamedTypeSymbol { IsGenericType: true } namedType
            && namedType.TypeArguments.Length == 1
            && namedType.OriginalDefinition.ToDisplayString() == "System.Func<TResult>")
        {
            innerType = namedType.TypeArguments[0];
            return true;
        }

        return false;
    }

    private static bool CheckIsEnumerable(ITypeSymbol parameterType, Compilation compilation)
    {
        var enumerableType = compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);

        // Check if the parameter type itself is IEnumerable<T>
        if (parameterType is INamedTypeSymbol { IsGenericType: true } namedType &&
            SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, enumerableType))
        {
            return true;
        }

        // Check if the parameter type implements IEnumerable<T>
        foreach (var interfaceType in parameterType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(interfaceType.OriginalDefinition, enumerableType))
            {
                return true;
            }
        }
        return false;
    }

    private static bool CheckIsLazy(ITypeSymbol parameterType, out ITypeSymbol? innerType)
    {
        if (parameterType is INamedTypeSymbol { IsGenericType: true, Arity: 1 } namedType
            && namedType.ConstructedFrom.ToDisplayString() == "System.Lazy<T>")
        {
            innerType = namedType.TypeArguments[0];
            return true;
        }

        innerType = null;
        return false;
    }

    private static string? GetServiceKeyFromAttributes(IParameterSymbol parameterSymbol, Compilation compilation)
    {
        var serviceKeyAttribute = compilation.GetTypeByMetadataName("Inject.NET.Attributes.ServiceKeyAttribute");
        foreach (var attr in parameterSymbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attr.AttributeClass, serviceKeyAttribute))
            {
                return attr.ConstructorArguments[0].Value as string;
            }
        }
        return null;
    }
}

public class Tenant
{
    public required INamedTypeSymbol TenantDefinition { get; init; }
    public required IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> RootDependencies { get; init; }
    public required IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> TenantDependencies { get; init; }
}