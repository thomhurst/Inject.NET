using System;
using System.Collections.Generic;
using System.Linq;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

internal static class DependencyDictionary
{
    public static Dictionary<ISymbol?, ServiceModel[]> Create(Compilation compilation, AttributeData[] dependencyAttributes)
    {
        var list = new List<ServiceModelBuilder>();
        
        foreach (var attributeData in dependencyAttributes.OrderBy(x => x.ConstructorArguments.Length))
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

            var key = attributeData.NamedArguments.FirstOrDefault(x => x.Key == "Key").Value.Value as string;

            var lifetime = EnumPolyfill.Parse<Lifetime>(attributeData.AttributeClass!.Name.Replace("Attribute", string.Empty));

            var isGenericDefinition = serviceType.IsGenericDefinition();
            
            Add(compilation, 
                serviceType, 
                implementationType, 
                list,
                key,
                lifetime);

            if (isGenericDefinition)
            {
                var constructedTypes = GenericTypeHelper.GetConstructedTypes(compilation, serviceType);

                foreach (var constructedType in constructedTypes)
                {
                    if(!list.Any(x => SymbolEqualityComparer.Default.Equals(x.ServiceType, constructedType)))
                    {
                        Add(compilation,
                            constructedType,
                            implementationType.IsGenericType
                                ? implementationType.OriginalDefinition.Construct([..constructedType.TypeArguments])
                                : implementationType,
                            list,
                            key,
                            lifetime);
                    }
                }
            }
        }
        
        // TODO Merge with tenant ones too
        foreach (var parameter in list.SelectMany(x => x.Parameters).ToList())
        {
            if (list.Any(x =>
                    SymbolEqualityComparer.Default.Equals(x.ServiceType, parameter.Type)))
            {
                continue;
            }

            if (parameter.Type is not INamedTypeSymbol { IsGenericType: true } namedParameterType)
            {
                continue;
            }
                
            if (list.Find(x => SymbolEqualityComparer.Default.Equals(x.ServiceType, namedParameterType.ConstructUnboundGenericType())) is {} found)
            {
                var implementationType = found.ImplementationType.OriginalDefinition.Construct([..namedParameterType.TypeArguments]);

                if (!namedParameterType.IsGenericDefinition()
                    && !implementationType.IsGenericDefinition())
                {
                    list.Add(found with
                    {
                        ServiceType = namedParameterType,
                        ImplementationType = implementationType,
                        IsOpenGeneric = false,
                    });
                }
            }
        }

        var enumerableDictionaryBuilder = list
            .GroupBy(x => x.ServiceType, SymbolEqualityComparer.Default)
            .ToDictionary(
                x => x.Key,
                x => x.ToArray(), SymbolEqualityComparer.Default);

        var enumerableDictionary = enumerableDictionaryBuilder
            .ToDictionary(
                x => x.Key,
                x => x.Value.Select(smb => new ServiceModel
                {
                    ServiceType = smb.ServiceType,
                    ImplementationType = smb.ImplementationType,
                    Key = smb.Key,
                    Lifetime = smb.Lifetime,
                    IsOpenGeneric = smb.IsOpenGeneric,
                    Parameters = smb.Parameters.ToArray()
                }).ToArray(), SymbolEqualityComparer.Default);
        
        return enumerableDictionary;
    }

    private static void Add(Compilation compilation, INamedTypeSymbol serviceType, INamedTypeSymbol implementationType,
        List<ServiceModelBuilder> list, string? key, Lifetime lifetime)
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
            Lifetime = lifetime
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
        
        var parameters = namedTypeSymbol
            ?.InstanceConstructors
            .FirstOrDefault(x => !x.IsImplicitlyDeclared)
            ?.Parameters ?? default;

        if (parameters.IsDefaultOrEmpty)
        {
            return [];
        }
        
        return parameters.Select(p => Map(p, compilation)).ToArray();
    }

    private static Parameter Map(IParameterSymbol parameterSymbol, Compilation compilation)
    {
        return new Parameter
        {
            Type = parameterSymbol.Type,
            IsEnumerable = parameterSymbol.Type.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T))),
            IsOptional = parameterSymbol.IsOptional,
            IsNullable = parameterSymbol.NullableAnnotation == NullableAnnotation.Annotated,
            Key = parameterSymbol.GetAttributes().FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, compilation.GetTypeByMetadataName("Inject.NET.Attributes.ServiceKeyAttribute")))?.ConstructorArguments[0].Value as string
        };
    }
}

internal class Tenant
{
    public Guid Guid { get; } = Guid.NewGuid();
    public required INamedTypeSymbol TenantDefinition { get; init; }
    public required string TenantId { get; init; }
    public required Dictionary<ISymbol?, ServiceModel[]> RootDependencies { get; init; }
    public required Dictionary<ISymbol?, ServiceModel[]> TenantDependencies { get; init; }
}