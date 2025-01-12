using System;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class ScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext,
        Compilation compilation, ServiceProviderInformation serviceProviderInformation)
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

        if (serviceProviderInformation.ServiceProviderType.ContainingNamespace is { IsGlobalNamespace: false })
        {
            sourceCodeWriter.WriteLine($"namespace {serviceProviderInformation.ServiceProviderType.ContainingNamespace.ToDisplayString()};");
            sourceCodeWriter.WriteLine();
        }
        
        var nestedClassCount = 0;
        var parent = serviceProviderInformation.ServiceProviderType.ContainingType;

        while (parent is not null)
        {
            nestedClassCount++;
            sourceCodeWriter.WriteLine($"public partial class {parent.Name}");
            sourceCodeWriter.WriteLine("{");
            parent = parent.ContainingType;
        }

        var className = $"{serviceProviderInformation.ServiceProviderType.Name}Scope";
        sourceCodeWriter.WriteLine($"public class {className} : ServiceScope<{serviceProviderInformation.ServiceProviderType.Name}SingletonScope>");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine($"public {className}(ServiceProviderRoot<{serviceProviderInformation.ServiceProviderType.GloballyQualified()}SingletonScope> root, {serviceProviderInformation.ServiceProviderType.GloballyQualified()}SingletonScope singletonScope, ServiceFactories serviceFactories) : base(root, singletonScope, serviceFactories)");
        sourceCodeWriter.WriteLine("{");
        sourceCodeWriter.WriteLine("}");

        foreach (var (_, serviceModels) in serviceProviderInformation.Dependencies)
        {
            var serviceModel = serviceModels.Last();

            if (serviceModel.Lifetime == Lifetime.Scoped || serviceModel.IsOpenGeneric)
            {
                continue;
            }
            
            sourceCodeWriter.WriteLine();
            sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
            var propertyName = PropertyNameHelper.Format(serviceModel);
            sourceCodeWriter.WriteLine($"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => field ??= Register({serviceModel.GetKey()}, {TypeHelper.WriteType(serviceProviderInformation.ServiceProviderType, serviceProviderInformation.Dependencies, serviceProviderInformation.ParentDependencies, serviceModel, Lifetime.Singleton)});");
        }
        
        foreach (var (_, serviceModels) in serviceProviderInformation.ParentDependencies
                     .Where(x => !serviceProviderInformation.Dependencies.Keys.Contains(x.Key, SymbolEqualityComparer.Default)))
        {
            var serviceModel = serviceModels.Last();

            if (serviceModel.Lifetime == Lifetime.Scoped || serviceModel.IsOpenGeneric)
            {
                continue;
            }
            
            var propertyName = PropertyNameHelper.Format(serviceModel);
            sourceCodeWriter.WriteLine($"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => Root.SingletonScope.{propertyName};");
        }
        
        sourceCodeWriter.WriteLine("}");
        
        for (var i = 0; i < nestedClassCount; i++)
        {
            sourceCodeWriter.WriteLine("}");
        }
        
        sourceProductionContext.AddSource($"{serviceProviderInformation.ServiceProviderType.Name}SingletonScope{Guid.NewGuid():N}.g.cs", sourceCodeWriter.ToString());
    }
}