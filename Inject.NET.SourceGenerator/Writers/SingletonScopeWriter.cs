using System;
using System.Collections.Generic;
using System.Linq;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class SingletonScopeWriter
{
    public static void Write(SourceProductionContext sourceProductionContext,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel,
        Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary, IEnumerable<Tenant> tenants)
    {
        var sourceCodeWriter = new SourceCodeWriter();
        
        sourceCodeWriter.WriteLine("using System;");
        sourceCodeWriter.WriteLine("using System.Linq;");
        sourceCodeWriter.WriteLine("using Inject.NET.Enums;");
        sourceCodeWriter.WriteLine("using Inject.NET.Extensions;");
        sourceCodeWriter.WriteLine("using Inject.NET.Interfaces;");
        sourceCodeWriter.WriteLine("using Inject.NET.Models;");
        sourceCodeWriter.WriteLine("using Inject.NET.Services;");
        sourceCodeWriter.WriteLine();

        if (serviceProviderModel.Type.ContainingNamespace is { IsGlobalNamespace: false })
        {
            sourceCodeWriter.WriteLine($"namespace {serviceProviderModel.Type.ContainingNamespace.ToDisplayString()};");
            sourceCodeWriter.WriteLine();
        }
        
        var nestedClassCount = 0;
        var parent = serviceProviderModel.Type.ContainingType;

        while (parent is not null)
        {
            nestedClassCount++;
            sourceCodeWriter.WriteLine($"public partial class {parent.Name}");
            sourceCodeWriter.WriteLine("{");
            parent = parent.ContainingType;
        }

        sourceCodeWriter.WriteLine(
            $"public class {serviceProviderModel.Type.Name}SingletonScope : SingletonScope");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine($"public {serviceProviderModel.Type.Name}SingletonScope(IServiceProviderRoot root, ServiceFactories serviceFactories) : base(root, serviceFactories)");
        sourceCodeWriter.WriteLine("{");
        
        var singletons = WriteRegistrations(serviceProviderModel.Type, dependencyDictionary, sourceCodeWriter, false);
        
        foreach (var tenant in tenants)
        {
            sourceCodeWriter.WriteLine("{");
            
            sourceCodeWriter.WriteLine($"var tenant = GetOrCreateTenant(\"{tenant.TenantId}\");");
            
            WriteRegistrations(serviceProviderModel.Type, tenant.TenantDependencies, sourceCodeWriter, true);

            sourceCodeWriter.WriteLine("}");
        }

        sourceCodeWriter.WriteLine("}");
        
        foreach (var (_, singleton) in singletons)
        {
            sourceCodeWriter.WriteLine($$"""
                                         public global::System.Lazy<{{singleton.ServiceType.GloballyQualified()}}> {{PropertyNameHelper.Format(singleton)}} { get; }
                                         """);
        }

        sourceCodeWriter.WriteLine("}");
        
        for (var i = 0; i < nestedClassCount; i++)
        {
            sourceCodeWriter.WriteLine("}");
        }
        
        sourceProductionContext.AddSource($"{serviceProviderModel.Type.Name}SingletonScope{Guid.NewGuid():N}.g.cs", sourceCodeWriter.ToString());
    }

    private static KeyValuePair<ISymbol, ServiceModel>[] WriteRegistrations(INamedTypeSymbol serviceProviderType, Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary, SourceCodeWriter sourceCodeWriter, bool isTenant)
    {
        var prefix = isTenant ? "tenant." : null;
        
        var singletons = dependencyDictionary.Where(x => x.Value[^1].Lifetime is Lifetime.Singleton)
            .Select(x => new KeyValuePair<ISymbol, ServiceModel>(x.Key!, x.Value[^1]))
            .Where(x => !x.Value.ServiceType.IsGenericDefinition())
            .ToArray();

        if (!isTenant)
        {
            foreach (var (_, singleton) in singletons)
            {
                sourceCodeWriter.WriteLine(
                    $"{PropertyNameHelper.Format(singleton)} = new global::System.Lazy<{singleton.ServiceType.GloballyQualified()}>(() => {WriteSingleton(singleton, dependencyDictionary)});");
            }
        }

        foreach (var (_, singleton) in singletons)
        {
            var propertyName = PropertyNameHelper.Format(singleton);

            var key = singleton.Key is null ? "null" : $"\"{singleton.Key}\"";
            
            sourceCodeWriter.WriteLine($"{prefix}Register(new global::Inject.NET.Models.ServiceKey(typeof({singleton.ServiceType.GloballyQualified()}), {key}), new global::System.Lazy<object>(() => {propertyName}.Value));");
        }

        return singletons;
    }

    private static string WriteSingleton(ServiceModel singleton,
        Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary)
    {
        return $"new {singleton.ImplementationType.GloballyQualified()}({string.Join(", ", GetParameters(singleton, dependencyDictionary))})";
    }

    private static IEnumerable<string> GetParameters(ServiceModel serviceModel,
        Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary)
    {
        return serviceModel.Parameters.Select(parameter =>
        {
            if (!dependencyDictionary.ContainsKey(parameter.Type))
            {
                return $"global::Inject.NET.ThrowHelpers.Throw<{parameter.Type.GloballyQualified()}>(\"No dependency found for {parameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)} when trying to construct {serviceModel.ImplementationType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)}\")";
            }
            
            return $"{PropertyNameHelper.Format(parameter.Type)}.Value";
        });
    }

    private static void WriteWithTenant(SourceCodeWriter sourceCodeWriter, INamedTypeSymbol serviceProviderType, Compilation compilation, string tenantId,
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
        
        WriteRegistration(sourceCodeWriter, serviceProviderType, dependencyDictionary, "tenant.");
        
        WriteTenantOverrides(sourceCodeWriter, serviceProviderType, rootDependencyDictionary, dependencyDictionary);
        
        sourceCodeWriter.WriteLine("}");
    }

    private static void WriteTenantOverrides(SourceCodeWriter sourceCodeWriter,
        INamedTypeSymbol serviceProviderType,
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
            WriteRegistration(sourceCodeWriter, serviceProviderType, mergedDictionaries, "tenant.", serviceModel);
        }
    }

    private static void WriteRegistration(SourceCodeWriter sourceCodeWriter, INamedTypeSymbol serviceProviderType,
        Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary, string prefix)
    {
        foreach (var (_, serviceModels) in dependencyDictionary)
        {
            foreach (var serviceModel in serviceModels)
            {
                WriteRegistration(sourceCodeWriter, serviceProviderType, dependencyDictionary, prefix, serviceModel);
            }
        }
    }

    private static void WriteRegistration(SourceCodeWriter sourceCodeWriter, INamedTypeSymbol serviceProviderType, Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary, string prefix,
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
                
        sourceCodeWriter.WriteLine(ObjectConstructionHelper.ConstructNewObject(serviceProviderType, dependencyDictionary, serviceModel));
                
        sourceCodeWriter.WriteLine("});");
        sourceCodeWriter.WriteLine();
    }
}