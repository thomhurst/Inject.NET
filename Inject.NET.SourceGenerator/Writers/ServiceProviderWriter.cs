using System.Globalization;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class ServiceProviderWriter
{
    public static void Write(SourceProductionContext sourceProductionContext,
        TypedServiceProviderModel serviceProviderModel, TenantedServiceProviderInformation serviceProviderInformation,
        IEnumerable<Tenant> tenants)
    {
        NestedServiceWrapperWriter.Wrap(sourceProductionContext, serviceProviderModel,
            sourceCodeWriter =>
            {
                var serviceProviderType = serviceProviderInformation.ServiceProviderType;
                
                sourceCodeWriter.WriteLine(
                    $"{serviceProviderType.DeclaredAccessibility.ToString().ToLower(CultureInfo.InvariantCulture)} class ServiceProvider(global::Inject.NET.Models.ServiceFactories serviceFactories, global::System.Collections.Generic.IDictionary<string, IServiceRegistrar> tenantRegistrars) : global::Inject.NET.Services.ServiceProviderRoot<ServiceProvider, SingletonScope>(serviceFactories, tenantRegistrars)");
                sourceCodeWriter.WriteLine("{");

                sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
                sourceCodeWriter.WriteLine(
                    """public override SingletonScope SingletonScope => field ??= new(this, serviceFactories);""");

                sourceCodeWriter.WriteLine(
                    "public override IServiceScope CreateScope() => new Scope(this, SingletonScope, ServiceFactories);");
                
                foreach (var tenant in tenants)
                {
                    sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
                    
                    // TODO: Async?
                    sourceCodeWriter.WriteLine($$"""public IServiceProvider Tenant{{tenant.Guid}} { get; private set; } = null!;""");
                }
                
                sourceCodeWriter.WriteLine("""public override async ValueTask InitializeAsync()""");
                sourceCodeWriter.WriteLine("{");
                sourceCodeWriter.WriteLine("await base.InitializeAsync();");

                foreach (var tenant in tenants)
                {
                    sourceCodeWriter.WriteLine($"Tenant{tenant.Guid} = new TenantServiceProvider<ServiceProvider, SingletonScope>(this, SingletonScope);");
                    sourceCodeWriter.WriteLine($"Register(\"{tenant.TenantId}\", Tenant{tenant.Guid});");
                }

                sourceCodeWriter.WriteLine("}");

                sourceCodeWriter.WriteLine("}");
            });
    }
}