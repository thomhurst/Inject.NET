using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class StaticWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, TypedServiceProviderModel typedServiceProviderModel)
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

        var serviceProviderType = typedServiceProviderModel.Type;
        
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

        sourceCodeWriter.WriteLine($"public partial class {serviceProviderType.Name}");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine(
            $"public static ValueTask<{serviceProviderType.Name}{typedServiceProviderModel.Id}.ServiceProvider> BuildAsync() =>");
        sourceCodeWriter.WriteLine($"\tnew {serviceProviderType.Name}{typedServiceProviderModel.Id}.ServiceRegistrar().BuildAsync();");
        
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