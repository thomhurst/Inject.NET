using System.Globalization;
using System.Text;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

public static class ServiceProviderWriter
{
    public static void GenerateServiceProviderCode(SourceProductionContext sourceProductionContext, (TypedServiceProviderModel ServiceProviderModel, Compilation Compilation) tuple)
    {
        ServiceRegistrarWriter.GenerateServiceRegistrarCode(sourceProductionContext, tuple.Compilation, tuple.ServiceProviderModel);
        
        var sourceCodeWriter = new SourceCodeWriter();
        
        sourceCodeWriter.WriteLine("using System;");
        sourceCodeWriter.WriteLine("using System.Threading.Tasks;");
        sourceCodeWriter.WriteLine("using Inject.NET.Enums;");
        sourceCodeWriter.WriteLine("using Inject.NET.Interfaces;");
        sourceCodeWriter.WriteLine();
        
        var serviceProviderType = tuple.ServiceProviderModel.Type;
        
        sourceCodeWriter.WriteLine($"namespace {serviceProviderType.ContainingNamespace.ToDisplayString()};");
        sourceCodeWriter.WriteLine();

        sourceCodeWriter.WriteLine(
            $"{serviceProviderType.DeclaredAccessibility.ToString().ToLower(CultureInfo.InvariantCulture)} partial class {serviceProviderType.Name}");
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine("public static Task<ITenantedServiceProvider> BuildAsync() =>");
        sourceCodeWriter.WriteLine($"new {serviceProviderType.Name}ServiceRegistrar().BuildAsync();");

        sourceCodeWriter.WriteLine("}");

        sourceProductionContext.AddSource(
            $"{serviceProviderType.Name}ServiceProvider.g.cs",
            sourceCodeWriter.ToString()
        );
    }
}