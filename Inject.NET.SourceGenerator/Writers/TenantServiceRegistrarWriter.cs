using Inject.NET.SourceGenerator.Helpers;
using Inject.NET.SourceGenerator.Models;

namespace Inject.NET.SourceGenerator.Writers;

internal static class TenantServiceRegistrarWriter
{
    public static void Write(SourceCodeWriter sourceCodeWriter, TypedServiceProviderModel serviceProviderModel, TenantServiceModelCollection tenantServices)
    {
        var tenantName = tenantServices.TenantName;
        
        sourceCodeWriter.WriteLine(
            $"public partial class ServiceRegistrar{tenantName} : global::Inject.NET.Services.ServiceRegistrar<ServiceProvider_{tenantName}, {serviceProviderModel.Prefix}ServiceProvider_>");

        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine($"public ServiceRegistrar{tenantName}()");
        sourceCodeWriter.WriteLine("{");

        WriteRegistration(sourceCodeWriter, tenantServices, string.Empty);

        // Call user-defined configuration hook for extension method registrations
        sourceCodeWriter.WriteLine("ConfigureServices();");

        sourceCodeWriter.WriteLine("}");

        sourceCodeWriter.WriteLine();

        // Declare partial method that users can implement for extension-based service registration
        sourceCodeWriter.WriteLine("partial void ConfigureServices();");

        sourceCodeWriter.WriteLine();

        sourceCodeWriter.WriteLine($$"""
                                     public override async ValueTask<ServiceProvider_{{tenantName}}> BuildAsync({{serviceProviderModel.Prefix}}ServiceProvider_ parentServiceProvider)
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

        string baseInvocation;
        if (serviceModel.IsOpenGeneric)
        {
            var constructorParams = string.Join(", ", BuildOpenGenericParameters(serviceModel));
            if (string.IsNullOrEmpty(constructorParams))
            {
                baseInvocation = $"Activator.CreateInstance(typeof({serviceModel.ImplementationType.GloballyQualified()}).MakeGenericType(type.GenericTypeArguments))";
            }
            else
            {
                baseInvocation = $"Activator.CreateInstance(typeof({serviceModel.ImplementationType.GloballyQualified()}).MakeGenericType(type.GenericTypeArguments), {constructorParams})";
            }
        }
        else
        {
            var lastTypeInDictionary = tenantServices.Services[serviceModel.ServiceKey][^1];

            baseInvocation = $"new {lastTypeInDictionary.ImplementationType.GloballyQualified()}({string.Join(", ", BuildParameters(serviceModel))})";
        }

        // Check if method injection is needed
        if (MethodInjectionHelper.HasInjectMethods(serviceModel))
        {
            sourceCodeWriter.WriteLine("Factory = (scope, type, key) =>");
            sourceCodeWriter.WriteLine("{");
            sourceCodeWriter.WriteLine($"var __instance = {baseInvocation};");

            foreach (var injectCall in MethodInjectionHelper.GenerateFactoryInjectCalls(serviceModel, "__instance"))
            {
                sourceCodeWriter.WriteLine(injectCall);
            }

            sourceCodeWriter.WriteLine("return __instance;");
            sourceCodeWriter.WriteLine("}");
        }
        else
        {
            sourceCodeWriter.WriteLine("Factory = (scope, type, key) =>");
            sourceCodeWriter.WriteLine(baseInvocation);
        }

        sourceCodeWriter.WriteLine("});");
        sourceCodeWriter.WriteLine();
    }
    
    private static IEnumerable<string> BuildParameters(ServiceModel serviceModel)
    {
        foreach (var serviceModelParameter in serviceModel.Parameters)
        {
            if (serviceModelParameter.IsFunc && serviceModelParameter.FuncInnerType != null)
            {
                var innerType = serviceModelParameter.FuncInnerType;
                yield return $"new global::System.Func<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>())";
            }
            else
            {
                yield return $"scope.GetRequiredService<{serviceModelParameter.Type.GloballyQualified()}>()";
            }
        }
    }
    
    private static IEnumerable<string> BuildOpenGenericParameters(ServiceModel serviceModel)
    {
        var genericTypeParameters = serviceModel.ImplementationType.TypeParameters;

        foreach (var serviceModelParameter in serviceModel.Parameters)
        {
            // Handle Func<T> parameters
            if (serviceModelParameter.IsFunc && serviceModelParameter.FuncInnerType != null)
            {
                var innerType = serviceModelParameter.FuncInnerType;
                yield return $"new global::System.Func<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>())";
            }
            // Check if this parameter is a generic type parameter
            else if (serviceModelParameter.Type.TypeKind == Microsoft.CodeAnalysis.TypeKind.TypeParameter)
            {
                // Find the index of this type parameter in the generic type definition
                var parameterIndex = -1;
                for (int i = 0; i < genericTypeParameters.Length; i++)
                {
                    if (Microsoft.CodeAnalysis.SymbolEqualityComparer.Default.Equals(genericTypeParameters[i], serviceModelParameter.Type))
                    {
                        parameterIndex = i;
                        break;
                    }
                }

                if (parameterIndex >= 0)
                {
                    yield return $"scope.GetService(type.GenericTypeArguments[{parameterIndex}])";
                }
                else
                {
                    // Fallback to regular resolution if we can't find the type parameter
                    yield return $"scope.GetRequiredService<{serviceModelParameter.Type.GloballyQualified()}>()";
                }
            }
            else
            {
                // Non-generic parameter, resolve normally
                yield return $"scope.GetRequiredService<{serviceModelParameter.Type.GloballyQualified()}>()";
            }
        }
    }
}