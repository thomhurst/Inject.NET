using Inject.NET.SourceGenerator.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Inject.NET.SourceGenerator.Writers;

internal static class SingletonScopeWriter
{
    public static void Write(SourceCodeWriter sourceCodeWriter, TypedServiceProviderModel serviceProviderModel, RootServiceModelCollection rootServiceModelCollection, IDictionary<ServiceModelCollection.ServiceKey, List<DecoratorModel>> decorators = null)
    {
        sourceCodeWriter.WriteLine($"public class SingletonScope_ : global::Inject.NET.Services.SingletonScope<{serviceProviderModel.Prefix}SingletonScope_, {serviceProviderModel.Prefix}ServiceProvider_, {serviceProviderModel.Prefix}ServiceScope_, {serviceProviderModel.Prefix}SingletonScope_, {serviceProviderModel.Prefix}ServiceScope_, {serviceProviderModel.Prefix}ServiceProvider_>");
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine(
            $"public SingletonScope_({serviceProviderModel.Prefix}ServiceProvider_ serviceProvider, ServiceFactories serviceFactories) : base(serviceProvider, serviceFactories, null)");
        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine("}");

        // REDESIGN: Removed GetService override
        // Services now resolved purely through dictionary lookup in base class
        // Properties below still provide direct access optimization
        // sourceCodeWriter.WriteLine("public override object GetService(global::Inject.NET.Models.ServiceKey serviceKey, Inject.NET.Interfaces.IServiceScope originatingScope)");
        // sourceCodeWriter.WriteLine("{");
        // foreach (var serviceModel in singletonModels)
        // {
        //     sourceCodeWriter.WriteLine($"if (serviceKey == {serviceModel.GetNewServiceKeyInvocation()})");
        //     sourceCodeWriter.WriteLine("{");
        //     sourceCodeWriter.WriteLine($"return {serviceModel.GetPropertyName()};");
        //     sourceCodeWriter.WriteLine("}");
        // }
        // sourceCodeWriter.WriteLine();
        // sourceCodeWriter.WriteLine("return base.GetService(serviceKey, originatingScope);");
        // sourceCodeWriter.WriteLine("}");

        foreach (var (_, serviceModels) in rootServiceModelCollection.Services)
        {
            foreach (var serviceModel in serviceModels.Where(serviceModel => serviceModel.Lifetime == Lifetime.Singleton && !serviceModel.IsOpenGeneric))
            {
                sourceCodeWriter.WriteLine();
                var propertyName = serviceModel.GetPropertyName();

                // Use GetServices to ensure single code path for singleton creation
                // This delegates to the dictionary-based resolution which handles caching, parent delegation, etc.
                sourceCodeWriter.WriteLine(
                    $"public {serviceModel.ServiceType.GloballyQualified()} {propertyName} => ({serviceModel.ServiceType.GloballyQualified()})GetServices({serviceModel.GetNewServiceKeyInvocation()})[^1];");
            }
        }

        sourceCodeWriter.WriteLine("}");
    }
}