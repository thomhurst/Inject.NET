using System.Globalization;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class ServiceProviderWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, SourceCodeWriter sourceCodeWriter,
        TypedServiceProviderModel serviceProviderModel, TenantedServiceModelCollection tenantedServiceModelCollection,
        Tenant[] tenants)
    {
        var serviceProviderType = tenantedServiceModelCollection.ServiceProviderType;
                
        sourceCodeWriter.WriteLine(
            $"{serviceProviderType.DeclaredAccessibility.ToString().ToLower(CultureInfo.InvariantCulture)} class ServiceProvider_(global::Inject.NET.Models.ServiceFactories serviceFactories) : global::Inject.NET.Services.ServiceProvider<ServiceProvider_, SingletonScope_, ServiceScope_, ServiceProvider_, SingletonScope_, ServiceScope_>(serviceFactories, null)");
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
        sourceCodeWriter.WriteLine(
            "public override SingletonScope_ Singletons => field ??= new(this, serviceFactories);");

        sourceCodeWriter.WriteLine(
            "public override ServiceScope_ CreateTypedScope() => new ServiceScope_(this, serviceFactories);");
                
        foreach (var tenant in tenants)
        {
            sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
            
            sourceCodeWriter.WriteLine($$"""public ServiceProvider_{{tenant.TenantDefinition.Name}} Tenant_{{tenant.TenantDefinition.Name}} { get; private set; } = null!;""");
        }
                
        WriteInitializeAsync(sourceCodeWriter, tenantedServiceModelCollection, tenants);

        sourceCodeWriter.WriteLine("}");
    }

    private static void WriteInitializeAsync(SourceCodeWriter sourceCodeWriter,
        TenantedServiceModelCollection tenantedServiceModelCollection, Tenant[] tenants)
    {
        sourceCodeWriter.WriteLine("public override async ValueTask InitializeAsync()");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine("await Singletons.InitializeAsync();");

        sourceCodeWriter.WriteLine("await using var scope = CreateTypedScope();");
        
        foreach (var serviceModel in tenantedServiceModelCollection.Services.Where(x => x.Key is INamedTypeSymbol { IsUnboundGenericType: false }).Select(x => x.Value[^1]))
        {
            if(serviceModel.Lifetime == Lifetime.Singleton)
            {
                sourceCodeWriter.WriteLine($"_ = Singletons.{PropertyNameHelper.Format(serviceModel)};");
            }
            else if(serviceModel.Lifetime == Lifetime.Scoped)
            {
                sourceCodeWriter.WriteLine($"_ = scope.{PropertyNameHelper.Format(serviceModel)};");
            }
        }

        foreach (var tenant in tenants)
        {
            sourceCodeWriter.WriteLine($"Tenant_{tenant.TenantDefinition.Name} = await ServiceProvider_{tenant.TenantDefinition.Name}.BuildAsync(this);");
            sourceCodeWriter.WriteLine($"Register<{tenant.TenantDefinition.GloballyQualified()}>(Tenant_{tenant.TenantDefinition.Name});");
        }
        
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("await base.InitializeAsync();");

        sourceCodeWriter.WriteLine("}");
    }
}