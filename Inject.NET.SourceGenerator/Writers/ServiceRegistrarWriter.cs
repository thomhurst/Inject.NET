using System;
using System.Linq;
using System.Text;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

public static class ServiceRegistrarWriter
{
    public static void GenerateServiceRegistrarCode(SourceProductionContext sourceProductionContext,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel)
    {
        var attributes = serviceProviderModel.Type.GetAttributes();
        
        var sourceCodeWriter = new SourceCodeWriter();
        
        sourceCodeWriter.WriteLine("using System;");
        sourceCodeWriter.WriteLine("using Inject.NET.Enums;");
        sourceCodeWriter.WriteLine("using Inject.NET.Extensions;");
        sourceCodeWriter.WriteLine("using Inject.NET.Services;");

        sourceCodeWriter.WriteLine($"namespace {serviceProviderModel.Type.ContainingNamespace.ToDisplayString()};");

        sourceCodeWriter.WriteLine(
            $"public class {serviceProviderModel.Type.Name}ServiceRegistrar : ServiceRegistrar");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine($"public {serviceProviderModel.Type.Name}ServiceRegistrar()");
        sourceCodeWriter.WriteLine("{");
        
        foreach (var attributeData in attributes)
        {
            var attributeClass = attributeData.AttributeClass;

            if (attributeClass is null)
            {
                continue;
            }

            if (attributeClass.IsGenericType 
                && SymbolEqualityComparer.Default.Equals(attributeClass.OriginalDefinition,
                    compilation.GetTypeByMetadataName("Inject.NET.Attributes.WithTenantAttribute`1")))
            {
                WriteWithTenant(sourceCodeWriter, compilation, (string)attributeData.ConstructorArguments.First().Value!,
                    (INamedTypeSymbol)attributeClass.TypeArguments[0]);
                
                continue;
            }

            if(!TryGetServiceAndImplementation(attributeData, out var serviceType, out var implementationType))
            {
                continue;
            }

            if (serviceType is null || implementationType is null)
            {
                continue;
            }
            
            var parameters = GetParameters(implementationType, compilation);

            WriteRegistration(sourceCodeWriter, attributeData, serviceType, implementationType, parameters, null);
        }
        
        sourceCodeWriter.WriteLine("}");

        sourceCodeWriter.WriteLine("}");
        
        sourceProductionContext.AddSource($"{serviceProviderModel.Type.Name}ServiceRegistrar.g.cs", sourceCodeWriter.ToString());
    }

    private static void WriteWithTenant(SourceCodeWriter sourceCodeWriter, Compilation compilation, string tenantId,
        INamedTypeSymbol tenantDefinitionClass)
    {
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine($"var tenant = GetOrCreateTenant(\"{tenantId}\");");

        var attributes = tenantDefinitionClass.GetAttributes();
        
        foreach (var attributeData in attributes)
        {
            var attributeClass = attributeData.AttributeClass;

            if (attributeClass is null)
            {
                continue;
            }

            if(!TryGetServiceAndImplementation(attributeData, out var serviceType, out var implementationType))
            {
                continue;
            }

            if (serviceType is null || implementationType is null)
            {
                continue;
            }
            
            var parameters = GetParameters(implementationType, compilation);

            WriteRegistration(sourceCodeWriter, attributeData, serviceType, implementationType, parameters, "tenant.");
        }
        
        sourceCodeWriter.WriteLine("}");
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
                 {prefix}Register<{serviceType.GloballyQualified()}>((scope, type, key) => new {implementationType.GloballyQualified()}({string.Join(", ", parameters.Select(x => x.WriteSource()))}), Lifetime.{lifetime}, "{key}");
                 """);
        }
        else
        {
            sourceCodeWriter.WriteLine(
                $"""
                 {prefix}Register<{serviceType.GloballyQualified()}>((scope, type) => new {implementationType.GloballyQualified()}({string.Join(", ", parameters.Select(x => x.WriteSource()))}), Lifetime.{lifetime});
                 """);
        }
    }
    
    private static Parameter[] GetParameters(ITypeSymbol type, Compilation compilation)
    {
        var parameters = (type as INamedTypeSymbol)?
            .InstanceConstructors
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
            Type = parameterSymbol.Type.GloballyQualified(),
            IsEnumerable = parameterSymbol.Type.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T))),
            IsOptional = parameterSymbol.IsOptional || parameterSymbol.NullableAnnotation == NullableAnnotation.Annotated,
            Key = parameterSymbol.GetAttributes().FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, compilation.GetTypeByMetadataName("Inject.NET.Attributes.ServiceKeyAttribute")))?.ConstructorArguments[0].Value as string
        };
    }
}