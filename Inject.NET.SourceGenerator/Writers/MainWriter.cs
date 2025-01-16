using Inject.NET.SourceGenerator.Helpers;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class MainWriter
{
    public static void GenerateServiceProviderCode(SourceProductionContext sourceProductionContext, TypedServiceProviderModel serviceProviderModel, Compilation compilation)
    {
        var sourceCodeWriter = new SourceCodeWriter();
        
        sourceCodeWriter.WriteLine("using System;");
        sourceCodeWriter.WriteLine("using System.Diagnostics.CodeAnalysis;");
        sourceCodeWriter.WriteLine("using System.Linq;");
        sourceCodeWriter.WriteLine("using Inject.NET.Enums;");
        sourceCodeWriter.WriteLine("using Inject.NET.Extensions;");
        sourceCodeWriter.WriteLine("using Inject.NET.Interfaces;");
        sourceCodeWriter.WriteLine("using Inject.NET.Models;");
        sourceCodeWriter.WriteLine("using Inject.NET.Services;");
        sourceCodeWriter.WriteLine();

        var serviceProviderType = serviceProviderModel.Type;
        
        if (serviceProviderType.ContainingNamespace is { IsGlobalNamespace: false })
        {
            sourceCodeWriter.WriteLine($"namespace {serviceProviderType.ContainingNamespace.ToDisplayString()};");
            sourceCodeWriter.WriteLine();
        }

        var nestedClassCount = 0;
        var parent = serviceProviderType.ContainingType;

        while (parent is not null)
        {
            nestedClassCount++;
            sourceCodeWriter.WriteLine($"public partial class {parent.Name}");
            sourceCodeWriter.WriteLine("{");
            parent = parent.ContainingType;
        }
        
        var dependencyInjectionAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.IDependencyInjectionAttribute");

        var withTenantAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.WithTenantAttribute`1");
        
        var attributes = serviceProviderModel.Type.GetAttributes();
        
        var dependencyAttributes = attributes
            .Where(x => x.AttributeClass?.AllInterfaces.Contains(dependencyInjectionAttributeType,
                SymbolEqualityComparer.Default) == true)
            .ToArray();
        
        var withTenantAttributes = attributes
            .Where(x => x.AttributeClass?.IsGenericType is true && SymbolEqualityComparer.Default.Equals(withTenantAttributeType, x.AttributeClass.OriginalDefinition))
            .ToArray();

        var rootDependencies = DependencyDictionary.Create(compilation, dependencyAttributes);

        var tenants = TenantHelper.ConstructTenants(compilation, withTenantAttributes, rootDependencies);

        var serviceProviderInformation = TypeCollector.Collect(serviceProviderModel, compilation);
        
        foreach (var (_, value) in serviceProviderInformation.RootDependencies)
        {
            foreach (var serviceModel in value)
            {
                sourceProductionContext.CheckConflicts(serviceModel, serviceProviderInformation.Dependencies, serviceProviderInformation.ParentDependencies);
            }
        }
        
        foreach (var (_, value) in serviceProviderInformation.TenantDependencies)
        {
            foreach (var (_, value2) in value.Dependencies)
            {
                foreach (var serviceModel in value2)
                {
                    sourceProductionContext.CheckConflicts(serviceModel, value.Dependencies, value.ParentDependencies);
                }
            }
        }
        
        sourceCodeWriter.WriteLine($"public partial class {serviceProviderType.Name}");
        sourceCodeWriter.WriteLine("{");
        
        ServiceRegistrarWriter.Write(sourceProductionContext, sourceCodeWriter, compilation, serviceProviderModel, rootDependencies);
        SingletonScopeWriter.Write(sourceProductionContext, sourceCodeWriter, compilation, serviceProviderModel, serviceProviderInformation);
        ScopeWriter.Write(sourceProductionContext, sourceCodeWriter, compilation, serviceProviderModel, serviceProviderInformation);
        ServiceProviderWriter.Write(sourceProductionContext, sourceCodeWriter, serviceProviderModel, serviceProviderInformation, tenants);
        
        foreach (var tenant in tenants)
        {
            TenantServiceRegistrarWriter.Write(sourceProductionContext, sourceCodeWriter, compilation, serviceProviderModel, tenant);
            TenantSingletonScopeWriter.Write(sourceProductionContext, sourceCodeWriter, compilation, serviceProviderModel, serviceProviderInformation, tenant);
            TenantScopeWriter.Write(sourceProductionContext, sourceCodeWriter, compilation, serviceProviderModel, serviceProviderInformation, tenant);
            TenantServiceProviderWriter.Write(sourceProductionContext, sourceCodeWriter, serviceProviderModel, serviceProviderInformation, tenant);
        }
        
        sourceCodeWriter.WriteLine(
            $"public static ValueTask<ServiceProvider_> BuildAsync() =>");
        sourceCodeWriter.WriteLine($"\tnew ServiceRegistrar_().BuildAsync(null);");
        
        sourceCodeWriter.WriteLine("}");
        
        for (var i = 0; i < nestedClassCount; i++)
        {
            sourceCodeWriter.WriteLine("}");
        }

        sourceProductionContext.AddSource(
            $"{serviceProviderType.Name}ServiceProvider_{Guid.NewGuid():N}.g.cs",
            sourceCodeWriter.ToString()
        );
    }
}