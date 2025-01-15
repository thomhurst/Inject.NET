using System.Globalization;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class ServiceProviderWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, SourceCodeWriter sourceCodeWriter,
        TypedServiceProviderModel serviceProviderModel, TenantedServiceProviderInformation serviceProviderInformation,
        Tenant[] tenants)
    {
        var serviceProviderType = serviceProviderInformation.ServiceProviderType;
                
        sourceCodeWriter.WriteLine(
            $"{serviceProviderType.DeclaredAccessibility.ToString().ToLower(CultureInfo.InvariantCulture)} class ServiceProvider_(global::Inject.NET.Models.ServiceFactories serviceFactories) : global::Inject.NET.Services.ServiceProvider<ServiceProvider_, SingletonScope_, ServiceScope_, ServiceProvider_, SingletonScope_, ServiceScope_>(serviceFactories, null)");
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
        sourceCodeWriter.WriteLine(
            "public override SingletonScope_ SingletonScope => field ??= new(this, serviceFactories);");

        sourceCodeWriter.WriteLine(
            "public override ServiceScope_ CreateScope() => new ServiceScope_(this, serviceFactories);");
                
        foreach (var tenant in tenants)
        {
            sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
            
            sourceCodeWriter.WriteLine($$"""public ServiceProvider_{{tenant.Guid}} Tenant{{tenant.Guid}} { get; private set; } = null!;""");
        }
                
        WriteInitializeAsync(sourceCodeWriter, serviceProviderInformation, tenants);

        sourceCodeWriter.WriteLine("}");
    }

    private static void WriteInitializeAsync(SourceCodeWriter sourceCodeWriter,
        TenantedServiceProviderInformation serviceProviderInformation, Tenant[] tenants)
    {
        sourceCodeWriter.WriteLine("public override async ValueTask InitializeAsync()");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine("await using var scope = CreateScope();");
        foreach (var serviceModel in serviceProviderInformation.Dependencies.Where(x => x.Key is INamedTypeSymbol { IsUnboundGenericType: false }).Select(x => x.Value[^1]))
        {
            if(serviceModel.Lifetime == Lifetime.Singleton)
            {
                sourceCodeWriter.WriteLine($"_ = SingletonScope.{PropertyNameHelper.Format(serviceModel)};");
            }
            else if(serviceModel.Lifetime == Lifetime.Scoped)
            {
                sourceCodeWriter.WriteLine($"_ = scope.{PropertyNameHelper.Format(serviceModel)};");
            }
        }

        foreach (var tenant in tenants)
        {
            sourceCodeWriter.WriteLine($"Tenant{tenant.Guid} = await ServiceProvider_{tenant.Guid}.BuildAsync(this);");
            sourceCodeWriter.WriteLine($"Register(\"{tenant.TenantId}\", Tenant{tenant.Guid});");
        }
        
        sourceCodeWriter.WriteLine();
        sourceCodeWriter.WriteLine("await base.InitializeAsync();");

        sourceCodeWriter.WriteLine("}");
    }
}