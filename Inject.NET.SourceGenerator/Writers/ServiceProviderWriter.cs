using System.Globalization;
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
            $"{serviceProviderType.DeclaredAccessibility.ToString().ToLower(CultureInfo.InvariantCulture)} partial class {serviceProviderType.Name}");
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine("public static ValueTask<IServiceProviderRoot> BuildAsync() =>");
        sourceCodeWriter.WriteLine($"\tnew {serviceProviderType.Name}ServiceRegistrar().BuildAsync();");

        sourceCodeWriter.WriteLine("}");

        for (var i = 0; i < nestedClassCount; i++)
        {
            sourceCodeWriter.WriteLine("}");
        }

        sourceProductionContext.AddSource(
            $"{serviceProviderType.Name}ServiceProvider.g.cs",
            sourceCodeWriter.ToString()
        );
    }
}