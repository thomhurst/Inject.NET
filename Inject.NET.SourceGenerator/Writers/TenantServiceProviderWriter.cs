using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantServiceProviderWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, Tenant tenant, TypedServiceProviderModel serviceProviderModel, Compilation compilation)
    {
        var dependencyInjectionAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.IDependencyInjectionAttribute");

        var withTenantAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.WithTenantAttribute`1");
        
        var attributes = serviceProviderModel.Type
            .GetAttributes();
        
        var dependencyAttributes = attributes
            .Where(x => x.AttributeClass?.AllInterfaces.Contains(dependencyInjectionAttributeType,
                SymbolEqualityComparer.Default) == true)
            .ToArray();
        
        var withTenantAttributes = attributes
            .Where(x => x.AttributeClass?.IsGenericType is true && SymbolEqualityComparer.Default.Equals(withTenantAttributeType, x.AttributeClass))
            .ToArray();

        var rootDependencies = DependencyDictionary.Create(compilation, dependencyAttributes);

        var tenants = TenantHelper.ConstructTenants(compilation, withTenantAttributes, rootDependencies);
        
        var sourceCodeWriter = new SourceCodeWriter();
        
        sourceCodeWriter.WriteLine("using System;");
        sourceCodeWriter.WriteLine("using System.Threading.Tasks;");
        sourceCodeWriter.WriteLine("using Inject.NET.Enums;");
        sourceCodeWriter.WriteLine("using Inject.NET.Interfaces;");
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

        sourceCodeWriter.WriteLine(
            $"{serviceProviderType.DeclaredAccessibility.ToString().ToLower(CultureInfo.InvariantCulture)} partial class {serviceProviderType.Name} : global::Inject.NET.Services.ServiceProviderRoot");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine($$"""public override {{serviceProviderModel.Type.GloballyQualified()}}SingletonScope SingletonScope { get; }""");
        
        sourceCodeWriter.WriteLine($$"""
                                     public override IServiceScope CreateScope() => new {{serviceProviderModel.Type.Name}}TenantScope_{{tenant.Guid}}Scope(this, RootServiceProviderRoot.CreateScope(), SingletonScope, ServiceFactories);
                                     """);
        
        sourceCodeWriter.WriteLine(
            $"public {serviceProviderType.Name}(Inject.NET.Models.ServiceFactories serviceFactories, global::System.Collections.Generic.IDictionary<string, IServiceRegistrar> tenantRegistrars) : base(serviceFactories, tenantRegistrars)");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine("""SingletonScope = new(this, serviceFactories);""");
        
        sourceCodeWriter.WriteLine("}");

        sourceCodeWriter.WriteLine($"public static ValueTask<{serviceProviderModel.Type.GloballyQualified()}> BuildAsync() =>");
        sourceCodeWriter.WriteLine($"\tnew {serviceProviderType.Name}ServiceRegistrar().BuildAsync();");
        
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