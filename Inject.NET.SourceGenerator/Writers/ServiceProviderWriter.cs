using System.Globalization;
using Inject.NET.SourceGenerator.Models;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class ServiceProviderWriter
{
    public static void Write(SourceProductionContext sourceProductionContext, SourceCodeWriter sourceCodeWriter,
        TypedServiceProviderModel serviceProviderModel, RootServiceModelCollection rootServiceModelCollection,
        Tenant[] tenants)
    {
        var serviceProviderType = rootServiceModelCollection.ServiceProviderType;
                
        sourceCodeWriter.WriteLine(
            $"{serviceProviderType.DeclaredAccessibility.ToString().ToLower(CultureInfo.InvariantCulture)} class ServiceProvider_(global::Inject.NET.Models.ServiceFactories serviceFactories) : global::Inject.NET.Services.ServiceProvider<{serviceProviderModel.Prefix}ServiceProvider_, {serviceProviderModel.Prefix}SingletonScope_, {serviceProviderModel.Prefix}ServiceScope_, {serviceProviderModel.Prefix}ServiceProvider_, {serviceProviderModel.Prefix}SingletonScope_, {serviceProviderModel.Prefix}ServiceScope_>(serviceFactories, null)");
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine($"private {serviceProviderModel.Prefix}SingletonScope_? _singletons;");
        sourceCodeWriter.WriteLine(
            $"public override {serviceProviderModel.Prefix}SingletonScope_ Singletons => _singletons ??= new(this, serviceFactories);");

        sourceCodeWriter.WriteLine(
            $"public override {serviceProviderModel.Prefix}ServiceScope_ CreateTypedScope() => new(this, serviceFactories);");
                
        foreach (var tenant in tenants)
        {
            sourceCodeWriter.WriteLine($$"""public ServiceProvider_{{tenant.TenantDefinition.Name}} Tenant_{{tenant.TenantDefinition.Name}} { get; private set; } = null!;""");
        }
                
        WriteInitializeAsync(sourceCodeWriter, rootServiceModelCollection, tenants);

        sourceCodeWriter.WriteLine("}");
    }

    private static void WriteInitializeAsync(SourceCodeWriter sourceCodeWriter,
        RootServiceModelCollection rootServiceModelCollection, Tenant[] tenants)
    {
        sourceCodeWriter.WriteLine("public override async ValueTask InitializeAsync()");
        sourceCodeWriter.WriteLine("{");
        
        sourceCodeWriter.WriteLine("await Singletons.InitializeAsync();");

        sourceCodeWriter.WriteLine("await using var scope = CreateTypedScope();");
        
        foreach (var serviceModel in rootServiceModelCollection.Services.Where(x => x.Key.Type is INamedTypeSymbol { IsUnboundGenericType: false }).Select(x => x.Value[^1]))
        {
            if(serviceModel.Lifetime == Lifetime.Singleton)
            {
                sourceCodeWriter.WriteLine($"_ = Singletons.{NameHelper.AsProperty(serviceModel)};");
            }
            else if(serviceModel.Lifetime == Lifetime.Scoped)
            {
                sourceCodeWriter.WriteLine($"_ = scope.{NameHelper.AsProperty(serviceModel)};");
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