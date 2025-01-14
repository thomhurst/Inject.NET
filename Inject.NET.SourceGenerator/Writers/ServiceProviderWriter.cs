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
            $"{serviceProviderType.DeclaredAccessibility.ToString().ToLower(CultureInfo.InvariantCulture)} class ServiceProvider_(global::Inject.NET.Models.ServiceFactories serviceFactories) : global::Inject.NET.Services.ServiceProviderRoot<ServiceProvider_, SingletonScope_>(serviceFactories)");
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
        sourceCodeWriter.WriteLine(
            "public override SingletonScope_ SingletonScope => field ??= new(this, serviceFactories);");

        sourceCodeWriter.WriteLine(
            "public override IServiceScope CreateScope() => new ServiceScope_(this, SingletonScope, serviceFactories);");
                
        foreach (var tenant in tenants)
        {
            sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
            
            sourceCodeWriter.WriteLine($$"""public global::Inject.NET.Interfaces.IServiceProvider Tenant{{tenant.Guid}} { get; private set; } = null!;""");
        }
                
        sourceCodeWriter.WriteLine("public override async ValueTask InitializeAsync()");
        sourceCodeWriter.WriteLine("{");
        sourceCodeWriter.WriteLine("await base.InitializeAsync();");

        foreach (var tenant in tenants)
        {
            sourceCodeWriter.WriteLine($"Tenant{tenant.Guid} = await ServiceProvider_{tenant.Guid}.BuildAsync(this);");
            sourceCodeWriter.WriteLine($"Register(\"{tenant.TenantId}\", Tenant{tenant.Guid});");
        }

        sourceCodeWriter.WriteLine("}");

        sourceCodeWriter.WriteLine("}");
    }
}