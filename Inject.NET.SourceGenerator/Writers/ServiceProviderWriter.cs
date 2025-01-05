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
        
        var sourceCodeWriter = new StringBuilder();
        
        sourceCodeWriter.AppendLine("using System;");
        sourceCodeWriter.AppendLine("using System.Threading.Tasks;");
        sourceCodeWriter.AppendLine("using Inject.NET.Enums;");
        sourceCodeWriter.AppendLine("using Inject.NET.Interfaces;");

        var serviceProviderType = tuple.ServiceProviderModel.Type;
        
        sourceCodeWriter.AppendLine($"namespace {serviceProviderType.ContainingNamespace.ToDisplayString()};");

        sourceCodeWriter.AppendLine(
            $"{serviceProviderType.DeclaredAccessibility.ToString().ToLower(CultureInfo.InvariantCulture)} partial class {serviceProviderType.Name}");
        sourceCodeWriter.AppendLine("{");

        sourceCodeWriter.AppendLine("public static Task<ITenantedServiceProvider> BuildAsync() =>");
        sourceCodeWriter.AppendLine($"new {serviceProviderType.Name}ServiceRegistrar().BuildAsync();");

        sourceCodeWriter.AppendLine("}");

        sourceProductionContext.AddSource(
            $"{serviceProviderType.Name}ServiceProvider.g.cs",
            sourceCodeWriter.ToString()
        );
    }
}