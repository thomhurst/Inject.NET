using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class ServiceRegistrarWriter
{
    public static void Write(SourceProductionContext sourceProductionContext,
        Compilation compilation, TypedServiceProviderModel serviceProviderModel,
        Dictionary<ISymbol?, ServiceModel[]> dependencyDictionary)
    {

        NestedServiceWrapperWriter.Wrap(sourceProductionContext, serviceProviderModel,
            sourceCodeWriter =>
            {
                sourceCodeWriter.WriteLine(
                    "public class ServiceRegistrar : global::Inject.NET.Services.ServiceRegistrar<ServiceProvider>");
                
                sourceCodeWriter.WriteLine("{");

                sourceCodeWriter.WriteLine("public ServiceRegistrar()");
                sourceCodeWriter.WriteLine("{");

                WriteRegistration(sourceCodeWriter, serviceProviderModel.Type, dependencyDictionary, string.Empty);

                sourceCodeWriter.WriteLine("}");

                sourceCodeWriter.WriteLine();

                sourceCodeWriter.WriteLine("""
                                           public override async ValueTask<ServiceProvider> BuildAsync()
                                           {
                                               OnBeforeBuild(this);
                                           
                                               var serviceProvider = new ServiceProvider(ServiceFactoryBuilders.AsReadOnly());
                                               
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