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
            $"public class ServiceRegistrar{tenant.Guid} : global::Inject.NET.Services.TenantServiceRegistrar<ServiceRegistrar{tenant.Guid}, ServiceProvider_{tenant.Guid}, SingletonScope_{tenant.Guid}, ServiceProvider_, SingletonScope_, ServiceScope_>");

        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine($"public ServiceRegistrar{tenant.Guid}()");
        sourceCodeWriter.WriteLine("{");

        WriteRegistration(sourceCodeWriter, serviceProviderModel.Type, tenant.TenantDependencies,
            tenant.RootDependencies, string.Empty);

        sourceCodeWriter.WriteLine("}");

        sourceCodeWriter.WriteLine();

        sourceCodeWriter.WriteLine($$"""
                                     public override async ValueTask<ServiceProvider_{{tenant.Guid}}> BuildAsync(ServiceProvider_ rootServiceProvider)
                                     {
                                         var serviceProvider = new ServiceProvider_{{tenant.Guid}}(rootServiceProvider, ServiceFactoryBuilders.AsReadOnly());
                                         
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
        Dictionary<ISymbol?, ServiceModel[]> tenantDependencies,
        Dictionary<ISymbol?, ServiceModel[]> rootDependencies, string prefix)
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
        Dictionary<ISymbol?, ServiceModel[]> tenantDependencies, Dictionary<ISymbol?, ServiceModel[]> rootDependencies,
        string prefix,
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
        
        sourceCodeWriter.WriteLine(ObjectConstructionHelper.ConstructNewObject(serviceProviderType, tenantDependencies, rootDependencies, serviceModel, serviceModel.Lifetime));
                
        sourceCodeWriter.WriteLine("});");
        sourceCodeWriter.WriteLine();
    }
}