using System;
using System.Collections.Generic;
using System.Linq;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

public static class ServiceRegistrarWriter
{
    public static void GenerateServiceRegistrarCode(SourceProductionContext sourceProductionContext,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel)
    {
        var dependencyInjectionAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.IDependencyInjectionAttribute");
        var withTenantAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.WithTenantAttribute`1");

        var attributes = serviceProviderModel.Type
            .GetAttributes();
        
        var dependencyAttributes = attributes
            .Where(x => x.AttributeClass?.AllInterfaces.Contains(dependencyInjectionAttributeType,
                SymbolEqualityComparer.Default) == true)
            .ToArray();

        var dependencyDictionary = DependencyDictionary.Create(compilation, dependencyAttributes);
        
        var sourceCodeWriter = new SourceCodeWriter();
        
        sourceCodeWriter.WriteLine("using System;");
        sourceCodeWriter.WriteLine("using System.Linq;");
        sourceCodeWriter.WriteLine("using Inject.NET.Enums;");
        sourceCodeWriter.WriteLine("using Inject.NET.Extensions;");
        sourceCodeWriter.WriteLine("using Inject.NET.Services;");
        sourceCodeWriter.WriteLine();

        if (serviceProviderModel.Type.ContainingNamespace is { IsGlobalNamespace: false })
        {
            sourceCodeWriter.WriteLine($"namespace {serviceProviderModel.Type.ContainingNamespace.ToDisplayString()};");
            sourceCodeWriter.WriteLine();
        }

        sourceCodeWriter.WriteLine(
            $"public class {serviceProviderModel.Type.Name}ServiceRegistrar : ServiceRegistrar");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine($"public {serviceProviderModel.Type.Name}ServiceRegistrar()");
        sourceCodeWriter.WriteLine("{");

        WriteRegistration(sourceCodeWriter, dependencyDictionary, string.Empty);

        var withTenantAttributes = attributes.Where(x =>
            SymbolEqualityComparer.Default.Equals(x.AttributeClass?.OriginalDefinition, withTenantAttributeType));
        
        foreach (var withTenantAttribute in withTenantAttributes)
        {
            var tenantId = withTenantAttribute.ConstructorArguments[0].Value!.ToString();
            var tenantDefinitionClass = (INamedTypeSymbol) withTenantAttribute.AttributeClass!.TypeArguments[0];
            
            WriteWithTenant(sourceCodeWriter, compilation, tenantId, tenantDefinitionClass, dependencyDictionary);
        }
        
        sourceCodeWriter.WriteLine("}");

        sourceCodeWriter.WriteLine("}");
        
        sourceProductionContext.AddSource($"{serviceProviderModel.Type.Name}ServiceRegistrar_{Guid.NewGuid():N}.g.cs", sourceCodeWriter.ToString());
    }

    private static void WriteWithTenant(SourceCodeWriter sourceCodeWriter, Compilation compilation, string tenantId,
        INamedTypeSymbol tenantDefinitionClass, Dictionary<ISymbol?, ServiceModel[]> rootDependencyDictionary)
    {
        var dependencyInjectionAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.IDependencyInjectionAttribute");

        var dependencyAttributes = tenantDefinitionClass.GetAttributes()
            .Where(x => x.AttributeClass?.AllInterfaces.Contains(dependencyInjectionAttributeType,
                SymbolEqualityComparer.Default) == true)
            .ToArray();

        var dependencyDictionary = DependencyDictionary.Create(compilation, dependencyAttributes);
        
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine($"var tenant = GetOrCreateTenant(\"{tenantId}\");");
        
        WriteRegistration(sourceCodeWriter, dependencyDictionary, "tenant.");
        
        WriteTenantOverrides(sourceCodeWriter, rootDependencyDictionary, dependencyDictionary);
        
        sourceCodeWriter.WriteLine("}");
    }

    private static void WriteTenantOverrides(SourceCodeWriter sourceCodeWriter,
        Dictionary<ISymbol?, ServiceModel[]> rootDependencyDictionary,
        Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary)
    {
        var list = new List<(ISymbol?, ServiceModel)>();

        // If an object in the root dictionary has got parameters that have been overridden
        // We need to construct a new object for that tenant with the right instance
        foreach (var (key, serviceModels) in rootDependencyDictionary)
        {
            foreach (var serviceModel in serviceModels)
            {
                var parameters = serviceModel.GetAllNestedParameters(rootDependencyDictionary);

                foreach (var parameter in parameters)
                {
                    if (dependencyDictionary.TryGetValue(parameter.ServiceType, out _))
                    {
                        list.Add((key, serviceModel));
                    }
                }
            }
        }

        var dictionaryToOverride = list
            .GroupBy(x => x.Item1, SymbolEqualityComparer.Default)
            .ToDictionary(x => x.Key,
                x => x.Select(y => y.Item2).ToArray(),
                SymbolEqualityComparer.Default);

        var mergedDictionaries = rootDependencyDictionary
            .Concat(dictionaryToOverride)
            .Concat(dependencyDictionary)
                .GroupBy(x => x.Key, SymbolEqualityComparer.Default)
                .ToDictionary(x => x.Key,
                    x => x.SelectMany(y => y.Value).ToArray(), SymbolEqualityComparer.Default);
        
        foreach (var (_, serviceModel) in list)
        {
            WriteRegistration(sourceCodeWriter, mergedDictionaries, "tenant.", serviceModel);
        }
    }

    private static void WriteRegistration(SourceCodeWriter sourceCodeWriter,
        Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary, string prefix)
    {
        foreach (var (_, serviceModels) in dependencyDictionary)
        {
            foreach (var serviceModel in serviceModels)
            {
                WriteRegistration(sourceCodeWriter, dependencyDictionary, prefix, serviceModel);
            }
        }
    }

    private static void WriteRegistration(SourceCodeWriter sourceCodeWriter, Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary, string prefix,
        ServiceModel serviceModel)
    {
        sourceCodeWriter.WriteLine($"{prefix}Register(new global::Inject.NET.Models.ServiceDescriptor");
        sourceCodeWriter.WriteLine("{");
        sourceCodeWriter.WriteLine($"ServiceType = typeof({serviceModel.ServiceType.GloballyQualified()}),");
        sourceCodeWriter.WriteLine($"ImplementationType = typeof({serviceModel.ImplementationType.GloballyQualified()}),");
        sourceCodeWriter.WriteLine($"Lifetime = Inject.NET.Enums.Lifetime.{serviceModel.Lifetime.ToString()},");
                
        if(serviceModel.Key is not null)
        {
            sourceCodeWriter.WriteLine($"Key = \"{serviceModel.Key}\",");
        }
                
        sourceCodeWriter.WriteLine("Factory = (scope, type, key) =>");
                
        sourceCodeWriter.WriteLine(ConstructNewObject(dependencyDictionary, serviceModel));
                
        sourceCodeWriter.WriteLine("});");
        sourceCodeWriter.WriteLine();
    }

    private static IEnumerable<string> BuildParameters(Dictionary<ISymbol?,ServiceModel[]> dependencyDictionary, ServiceModel serviceModel)
    {
        foreach (var parameter in serviceModel.Parameters)
        {
            if (WriteParameter(dependencyDictionary, parameter, serviceModel) is { } written)
            {
                if(!parameter.Type.IsGenericDefinition())
                {
                    yield return $"({parameter.Type.GloballyQualified()}){written}";
                }
                else
                {
                    yield return written;
                }
            }
        }
    }

    private static string? WriteParameter(Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary,
        Parameter parameter, ServiceModel serviceModel)
    {
        ServiceModel[]? models = null;
        
        if (parameter.Type is ITypeParameterSymbol typeParameterSymbol)
        {
            var substitutedTypeIndex = serviceModel.ServiceType.TypeParameters.ToList()
                .FindIndex(x => x.Name == typeParameterSymbol.Name);

            if (substitutedTypeIndex != -1)
            {
                var subtitutedType = serviceModel.ServiceType.TypeArguments[substitutedTypeIndex];

                if (!dependencyDictionary.TryGetValue(subtitutedType, out models))
                {
                    var key = parameter.Key is null ? "null" : $"\"{parameter.Key}\"";

                    return parameter.IsOptional
                        ? $"scope.GetOptionalService<{subtitutedType.GloballyQualified()}>({key})"
                        : $"scope.GetRequiredService<{subtitutedType.GloballyQualified()}>({key})";
                }
            }
        }

        if (models is null && !dependencyDictionary.TryGetValue(parameter.Type, out models))
        {
            if (parameter.Type is not INamedTypeSymbol { IsGenericType: true } genericType
                || !dependencyDictionary.TryGetValue(genericType.ConstructUnboundGenericType(), out models))
            {
                if (parameter.IsOptional)
                {
                    return null;
                }

                if (parameter.IsNullable)
                {
                    return "null";
                }

                return null;
            }
        }

        if (parameter.IsEnumerable)
        {
            return $"scope.GetServices<{parameter.Type.GloballyQualified()}>({parameter.Key})";
        }

        var lastModel = models.Last();
        
        return WriteType(dependencyDictionary, lastModel, parameter);
    }

    private static string WriteType(Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary,
        ServiceModel serviceModel, Parameter parameter)
    {
        if (serviceModel.Lifetime == Lifetime.Transient)
        {
            return ConstructNewObject(dependencyDictionary, serviceModel);
        }

        if (serviceModel.Lifetime == Lifetime.Singleton)
        {
            return parameter.IsOptional 
                ? $"scope.SingletonScope.GetOptionalService<{serviceModel.ServiceType.GloballyQualified()}>({serviceModel.Key})" 
                : $"scope.SingletonScope.GetRequiredService<{serviceModel.ServiceType.GloballyQualified()}>({serviceModel.Key})";
        }
        
        return parameter.IsOptional 
            ? $"scope.GetOptionalService<{serviceModel.ServiceType.GloballyQualified()}>({serviceModel.Key})" 
            : $"scope.GetRequiredService<{serviceModel.ServiceType.GloballyQualified()}>({serviceModel.Key})";
    }

    private static string ConstructNewObject(Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary, ServiceModel serviceModel)
    {
        var lastTypeInDictionary = dependencyDictionary[serviceModel.ServiceType][^1];
        
        if (!serviceModel.IsOpenGeneric)
        {
            return
                $"new {lastTypeInDictionary.ImplementationType.GloballyQualified()}({string.Join(", ", BuildParameters(dependencyDictionary, serviceModel))})";
        }
        
        return $$"""
                 Activator.CreateInstance(typeof({{lastTypeInDictionary.ImplementationType.GloballyQualified()}}).MakeGenericType(type.GenericTypeArguments), [ ..type.GenericTypeArguments.Select(x => scope.GetService(x)) ])
                """;
    }
}