using Inject.NET.SourceGenerator.Models;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantServiceRegistrarWriter
{
    public static void Write(SourceCodeWriter sourceCodeWriter, TenantServiceModelCollection tenantServices)
    {
        var tenantName = tenantServices.TenantName;
        
        sourceCodeWriter.WriteLine(
            $"public class ServiceRegistrar{tenantName} : global::Inject.NET.Services.ServiceRegistrar<ServiceProvider_{tenantName}, ServiceProvider_>");

        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine($"public ServiceRegistrar{tenantName}()");
        sourceCodeWriter.WriteLine("{");

        WriteRegistration(sourceCodeWriter, tenantServices, string.Empty);

        sourceCodeWriter.WriteLine("}");

        sourceCodeWriter.WriteLine();

        sourceCodeWriter.WriteLine($$"""
                                     public override async ValueTask<ServiceProvider_{{tenantName}}> BuildAsync(ServiceProvider_? parentServiceProvider)
                                     {
                                         var serviceProvider = new ServiceProvider_{{tenantName}}(ServiceFactoryBuilders.AsReadOnly(), parentServiceProvider!);
                                         
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

    private static void WriteRegistration(SourceCodeWriter sourceCodeWriter, 
        TenantServiceModelCollection tenantServices, string prefix)
    {
        foreach (var (_, serviceModels) in tenantServices.Services)
        {
            foreach (var serviceModel in serviceModels.Where(x => !x.ResolvedFromParent))
            {
                WriteRegistration(sourceCodeWriter, tenantServices, prefix, serviceModel);
            }
        }
    }

    private static void WriteRegistration(SourceCodeWriter sourceCodeWriter,
        TenantServiceModelCollection tenantServices,
        string prefix,
        ServiceModel serviceModel)
    {
        sourceCodeWriter.WriteLine($"{prefix}Register(new global::Inject.NET.Models.ServiceDescriptor");
        sourceCodeWriter.WriteLine("{");
        sourceCodeWriter.WriteLine($"ServiceType = typeof({serviceModel.ServiceType.GloballyQualified()}),");
        sourceCodeWriter.WriteLine(
            $"ImplementationType = typeof({serviceModel.ImplementationType.GloballyQualified()}),");
        sourceCodeWriter.WriteLine($"Lifetime = Inject.NET.Enums.Lifetime.{serviceModel.Lifetime.ToString()},");

        if (serviceModel.Key is not null)
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
            var lastTypeInDictionary = tenantServices.Services[serviceModel.ServiceKey][^1];

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