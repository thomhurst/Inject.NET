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
            SymbolEqualityComparer.Default.Equals(x.AttributeClass, withTenantAttributeType));
        
        foreach (var withTenantAttribute in withTenantAttributes)
        {
            var tenantId = withTenantAttribute.ConstructorArguments[0].Value!.ToString();
            var tenantDefinitionClass = (INamedTypeSymbol) withTenantAttribute.AttributeClass!.TypeArguments[0];
            
            WriteWithTenant(sourceCodeWriter, compilation, tenantId, tenantDefinitionClass);
        }
        
        sourceCodeWriter.WriteLine("}");

        sourceCodeWriter.WriteLine("}");
        
        sourceProductionContext.AddSource($"{serviceProviderModel.Type.Name}ServiceRegistrar_{Guid.NewGuid():N}.g.cs", sourceCodeWriter.ToString());
    }

    private static void WriteWithTenant(SourceCodeWriter sourceCodeWriter, Compilation compilation, string tenantId,
        INamedTypeSymbol tenantDefinitionClass)
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
        
        sourceCodeWriter.WriteLine("}");
    }

    private static void WriteRegistration(SourceCodeWriter sourceCodeWriter,
        Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary, string prefix)
    {
        foreach (var (_, serviceModels) in dependencyDictionary)
        {
            foreach (var serviceModel in serviceModels)
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
        }
    }

    private static IEnumerable<string> BuildParameters(Dictionary<ISymbol?,ServiceModel[]> dependencyDictionary, ServiceModel serviceModel)
    {
        foreach (var parameter in serviceModel.Parameters)
        {
            if (WriteParameter(dependencyDictionary, parameter) is { } written)
            {
                yield return written;
            }
        }
    }

    private static string? WriteParameter(Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary, ServiceModelParameter parameter)
    {
        if (!dependencyDictionary.TryGetValue(parameter.Type, out var models))
        {
            if (parameter.IsOptional)
            {
                return null;
            }

            if (parameter.IsNullable)
            {
                return "null";
            }

            throw new Exception($"No model found for {parameter.Type.GloballyQualified()}");
        }

        if (parameter.IsEnumerable)
        {
            return $"scope.GetServices<{parameter.Type.GloballyQualified()}>({parameter.Key})";
        }

        var lastModel = models.Last();
        
        return WriteType(dependencyDictionary, lastModel, parameter);
    }

    private static string WriteType(Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary,
        ServiceModel serviceModel, ServiceModelParameter parameter)
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
        if (!serviceModel.IsOpenGeneric)
        {
            return
                $"new {serviceModel.ImplementationType.GloballyQualified()}({string.Join(", ", BuildParameters(dependencyDictionary, serviceModel))})";
        }

        return $"Activator.CreateInstance(typeof({serviceModel.ImplementationType.GloballyQualified()}).MakeGenericType(type.GenericTypeArguments), [{string.Join(", ", BuildParameters(dependencyDictionary, serviceModel))}])";
    }
}