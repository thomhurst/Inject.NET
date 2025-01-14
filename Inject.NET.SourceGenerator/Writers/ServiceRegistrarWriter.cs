using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class ServiceRegistrarWriter
{
    public static void Write(SourceProductionContext sourceProductionContext,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel,
        Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary)
    {
        var withTenantAttributeType = compilation.GetTypeByMetadataName("Inject.NET.Attributes.WithTenantAttribute`1");

        NestedServiceWrapperWriter.Wrap(sourceProductionContext, serviceProviderModel.Type,
            sourceCodeWriter =>
            {
                sourceCodeWriter.WriteLine(
                    $"public class ServiceRegistrar : ServiceRegistrar<{serviceProviderModel.Type.GloballyQualified()}, {serviceProviderModel.Type.GloballyQualified()}SingletonScope>");
                
                sourceCodeWriter.WriteLine("{");

                sourceCodeWriter.WriteLine($"public ServiceRegistrar()");
                sourceCodeWriter.WriteLine("{");

                WriteRegistration(sourceCodeWriter, serviceProviderModel.Type, dependencyDictionary, string.Empty);

                var withTenantAttributes = serviceProviderModel.Type.GetAttributes().Where(x =>
                    SymbolEqualityComparer.Default.Equals(x.AttributeClass?.OriginalDefinition,
                        withTenantAttributeType));

                foreach (var withTenantAttribute in withTenantAttributes)
                {
                    var tenantId = withTenantAttribute.ConstructorArguments[0].Value!.ToString();
                    var tenantDefinitionClass = (INamedTypeSymbol)withTenantAttribute.AttributeClass!.TypeArguments[0];

                    WriteWithTenant(sourceCodeWriter, serviceProviderModel.Type, compilation, tenantId,
                        tenantDefinitionClass, dependencyDictionary);
                }

                sourceCodeWriter.WriteLine("}");

                sourceCodeWriter.WriteLine();

                sourceCodeWriter.WriteLine($$"""
                                             public override async ValueTask<{{serviceProviderModel.Type.GloballyQualified()}}> BuildAsync()
                                             {
                                                 OnBeforeBuild(this);
                                             
                                                 var serviceProvider = new {{serviceProviderModel.Type.GloballyQualified()}}(ServiceFactoryBuilders.AsReadOnly(), Tenants);
                                                 
                                                 var vt = serviceProvider.InitializeAsync();
                                             
                                                 if (!vt.IsCompletedSuccessfully)
                                                 {
                                                     await vt.ConfigureAwait(false);
                                                 }
                                                 
                                                 return serviceProvider;
                                             }
                                             """);

                sourceCodeWriter.WriteLine("}");
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

    private static void WriteRegistration(SourceCodeWriter sourceCodeWriter,
        INamedTypeSymbol serviceProviderType, Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary, string prefix)
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
                
        sourceCodeWriter.WriteLine(ObjectConstructionHelper.ConstructNewObject(serviceProviderType, dependencyDictionary, [], serviceModel, serviceModel.Lifetime));
                
        sourceCodeWriter.WriteLine("});");
        sourceCodeWriter.WriteLine();
    }
}