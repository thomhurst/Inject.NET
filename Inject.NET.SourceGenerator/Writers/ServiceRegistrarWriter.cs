using Inject.NET.SourceGenerator.Models;

namespace Inject.NET.SourceGenerator.Writers;

internal static class ServiceRegistrarWriter
{
    public static void Write(SourceCodeWriter sourceCodeWriter, TypedServiceProviderModel serviceProviderModel,
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencyDictionary)
    {
        sourceCodeWriter.WriteLine(
            $"public class ServiceRegistrar_ : global::Inject.NET.Services.ServiceRegistrar<{serviceProviderModel.Prefix}ServiceProvider_, {serviceProviderModel.Prefix}ServiceProvider_>");
                
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine("public ServiceRegistrar_()");
        sourceCodeWriter.WriteLine("{");

        WriteRegistration(sourceCodeWriter, dependencyDictionary, string.Empty);

        sourceCodeWriter.WriteLine("}");

        sourceCodeWriter.WriteLine();

        sourceCodeWriter.WriteLine($$"""
                                     public override async ValueTask<{{serviceProviderModel.Prefix}}ServiceProvider_> BuildAsync({{serviceProviderModel.Prefix}}ServiceProvider_ parent)
                                     {
                                         var serviceProvider = new {{serviceProviderModel.Prefix}}ServiceProvider_(ServiceFactoryBuilders.AsReadOnly());
                                         
                                         var vt = serviceProvider.InitializeAsync();
                                     
                                         if (!vt.IsCompletedSuccessfully)
                                         {
                                             await vt.ConfigureAwait(false);
                                         }
                                         
                                         return serviceProvider;
                                     }
                                     """);

        sourceCodeWriter.WriteLine("}");
    }

    private static void WriteRegistration(SourceCodeWriter sourceCodeWriter, IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencyDictionary, string prefix)
    {
        foreach (var (_, serviceModels) in dependencyDictionary)
        {
            foreach (var serviceModel in serviceModels.Where(x => !x.ResolvedFromParent))
            {
                WriteRegistration(sourceCodeWriter, dependencyDictionary, prefix, serviceModel);
            }
        }
    }

    private static void WriteRegistration(SourceCodeWriter sourceCodeWriter, IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencyDictionary, string prefix,
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

        if (serviceModel.IsOpenGeneric)
        {
            sourceCodeWriter.WriteLine("scope.GetRequiredService(type)");
        }
        else
        {
            var lastTypeInDictionary = dependencyDictionary[serviceModel.ServiceKey][^1];

            sourceCodeWriter.WriteLine(
                $"new {lastTypeInDictionary.ImplementationType.GloballyQualified()}({string.Join(", ", BuildParameters(serviceModel))})");
        }

        sourceCodeWriter.WriteLine("});");
        sourceCodeWriter.WriteLine();
    }
    
    private static IEnumerable<string> BuildParameters(ServiceModel serviceModel)
    {
        foreach (var serviceModelParameter in serviceModel.Parameters)
        {
            yield return $"scope.GetRequiredService<{serviceModelParameter.Type.GloballyQualified()}>()";
        }
    }
}