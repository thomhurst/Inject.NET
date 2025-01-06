using System;
using System.Collections.Generic;
using System.Linq;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator;

public static class DependencyDictionary
{
    public static Dictionary<ISymbol?, ServiceModel[]> Create(Compilation compilation, AttributeData[] attributes)
    {
        var list = new List<ServiceModelBuilder>();
        
        foreach (var attributeData in attributes)
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

            var parameters = GetParameters(implementationType, compilation);

            list.Add(new ServiceModelBuilder
            {
                ServiceType = serviceType,
                ImplementationType = implementationType,
                IsOpenGeneric = serviceType.IsGenericDefinition(),
                Parameters = parameters,
                Key = attributeData.NamedArguments.FirstOrDefault(x => x.Key == "Key").Value.Value as string,
                Lifetime = EnumPolyfill.Parse<Lifetime>(attributeData.AttributeClass!.Name.Replace("Attribute", string.Empty))
            });
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
                    Parameters = smb.Parameters.Select(p => new ServiceModelParameter
                    {
                        Type = p.Type,
                        Key = p.Key,
                        IsEnumerable = p.IsEnumerable,
                        IsNullable = p.IsNullable,
                        IsOptional = p.IsOptional
                    }).ToArray()
                }).ToArray(), SymbolEqualityComparer.Default);
        
        foreach (var serviceModels in enumerableDictionary.Values)
        {
            foreach (var serviceModel in serviceModels)
            {
                foreach (var parameter in serviceModel.Parameters)
                {
                    if (!enumerableDictionary.TryGetValue(parameter.Type, out var serviceModelsForParameterType))
                    {
                        if (parameter.IsOptional)
                        {
                            continue;
                        }
                        
                        // TODO: Error
                    }

                    parameter.ServiceModels.AddRange(serviceModelsForParameterType ?? []);
                }
            }
        }
        
        return enumerableDictionary;
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

    private static void WriteRegistration(SourceCodeWriter sourceCodeWriter, AttributeData attributeData,
        INamedTypeSymbol serviceType, INamedTypeSymbol implementationType, Parameter[] parameters, string? prefix)
    {
        var key = attributeData.NamedArguments.FirstOrDefault(x => x.Key == "Key").Value.Value as string;

        if (!Enum.TryParse(attributeData.AttributeClass?.Name.Replace("Attribute", string.Empty), false, out Lifetime lifetime))
        {
            return;
        }

        if (serviceType.IsGenericType 
            && SymbolEqualityComparer.Default.Equals(serviceType, serviceType.ConstructUnboundGenericType()))
        {
            if (key != null)
            {
                sourceCodeWriter.WriteLine(
                    $"""
                     {prefix}RegisterOpenGeneric(typeof({serviceType.GloballyQualified()}), typeof({implementationType.GloballyQualified()}), Lifetime.{lifetime}, "{key}");
                     """);
            }
            else
            {
                sourceCodeWriter.WriteLine(
                    $"""
                     {prefix}RegisterOpenGeneric(typeof({serviceType.GloballyQualified()}), typeof({implementationType.GloballyQualified()}), Lifetime.{lifetime});
                     """);
            }
            
            return;
        }
        
        if (key != null)
        {
            sourceCodeWriter.WriteLine(
                $"""
                 {prefix}Register<{serviceType.GloballyQualified()}, {implementationType.GloballyQualified()}>((scope, type, key) => new {implementationType.GloballyQualified()}({string.Join(", ", parameters.Select(x => x.WriteSource()))}), Lifetime.{lifetime}, "{key}");
                 """);
        }
        else
        {
            sourceCodeWriter.WriteLine(
                $"""
                 {prefix}Register<{serviceType.GloballyQualified()}, {implementationType.GloballyQualified()}>((scope, type) => new {implementationType.GloballyQualified()}({string.Join(", ", parameters.Select(x => x.WriteSource()))}), Lifetime.{lifetime});
                 """);
        }
    }
    
    private static Parameter[] GetParameters(ITypeSymbol type, Compilation compilation)
    {
        var namedTypeSymbol = type as INamedTypeSymbol;
        
        var parameters = namedTypeSymbol
            ?.InstanceConstructors
            .FirstOrDefault()
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