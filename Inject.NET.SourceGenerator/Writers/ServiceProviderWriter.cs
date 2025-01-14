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
        NestedServiceWrapperWriter.Wrap(sourceProductionContext, serviceProviderInformation.ServiceProviderType,
            sourceCodeWriter =>
            {
                var serviceProviderType = serviceProviderInformation.ServiceProviderType;
                
                sourceCodeWriter.WriteLine(
                    $"{serviceProviderType.DeclaredAccessibility.ToString().ToLower(CultureInfo.InvariantCulture)} partial class {serviceProviderType.Name} : global::Inject.NET.Services.ServiceProviderRoot<{serviceProviderModel.Type.GloballyQualified()}SingletonScope>");
                sourceCodeWriter.WriteLine("{");

                sourceCodeWriter.WriteLine("[field: AllowNull, MaybeNull]");
                sourceCodeWriter.WriteLine(
                    $$"""public override {{serviceProviderModel.Type.GloballyQualified()}}SingletonScope SingletonScope => field ??= new(this, serviceFactories);""");

                sourceCodeWriter.WriteLine(
                    $"public override IServiceScope CreateScope() => new {serviceProviderModel.Type.GloballyQualified()}Scope(this, SingletonScope, ServiceFactories);");

                sourceCodeWriter.WriteLine(
                    $"public {serviceProviderType.Name}(Inject.NET.Models.ServiceFactories serviceFactories, global::System.Collections.Generic.IDictionary<string, IServiceRegistrar> tenantRegistrars) : base(serviceFactories, tenantRegistrars)");
                sourceCodeWriter.WriteLine("{");
                sourceCodeWriter.WriteLine("}");
                
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
                    sourceCodeWriter.WriteLine($"Tenant{tenant.Guid} = new TenantServiceProvider<{serviceProviderModel.Type.GloballyQualified()}SingletonScope>(this, SingletonScope);");
                }

                sourceCodeWriter.WriteLine("}");
                sourceCodeWriter.WriteLine(
                    $"public static ValueTask<{serviceProviderModel.Type.GloballyQualified()}> BuildAsync() =>");
                sourceCodeWriter.WriteLine($"\tnew {serviceProviderType.Name}ServiceRegistrar().BuildAsync();");

                sourceCodeWriter.WriteLine("}");
            });
    }
}