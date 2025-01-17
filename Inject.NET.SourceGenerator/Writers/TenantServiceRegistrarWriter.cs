using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantServiceRegistrarWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, SourceCodeWriter sourceCodeWriter,
        Compilation compilation,
        TypedServiceProviderModel serviceProviderModel, Tenant tenant)
    {
        sourceCodeWriter.WriteLine(
            $"public class ServiceRegistrar{tenant.Guid} : global::Inject.NET.Services.ServiceRegistrar<ServiceProvider_{tenant.Guid}, ServiceProvider_>");

        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine($"public ServiceRegistrar{tenant.Guid}()");
        sourceCodeWriter.WriteLine("{");

        WriteRegistration(sourceCodeWriter, serviceProviderModel.Type, tenant.TenantDependencies,
            tenant.RootDependencies, string.Empty);

        sourceCodeWriter.WriteLine("}");

        sourceCodeWriter.WriteLine();

        sourceCodeWriter.WriteLine($$"""
                                     public override async ValueTask<ServiceProvider_{{tenant.Guid}}> BuildAsync(ServiceProvider_ parentServiceProvider)
                                     {
                                         var serviceProvider = new ServiceProvider_{{tenant.Guid}}(ServiceFactoryBuilders.AsReadOnly(), parentServiceProvider);
                                         
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
        INamedTypeSymbol serviceProviderType, 
        IDictionary<ISymbol?, List<ServiceModel>> tenantDependencies,
        IDictionary<ISymbol?, List<ServiceModel>> rootDependencies, string prefix)
    {
        foreach (var (_, serviceModels) in tenantDependencies)
        {
            foreach (var serviceModel in serviceModels)
            {
                WriteRegistration(sourceCodeWriter, serviceProviderType, tenantDependencies, rootDependencies, prefix, serviceModel);
            }
        }
    }

    private static void WriteRegistration(SourceCodeWriter sourceCodeWriter, INamedTypeSymbol serviceProviderType,
        IDictionary<ISymbol?, List<ServiceModel>> tenantDependencies, IDictionary<ISymbol?, List<ServiceModel>> rootDependencies,
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
            var lastTypeInDictionary = tenantDependencies[serviceModel.ServiceType][^1];

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