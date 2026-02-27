using Inject.NET.SourceGenerator.Helpers;
using Inject.NET.SourceGenerator.Models;

namespace Inject.NET.SourceGenerator.Writers;

internal static class ServiceRegistrarWriter
{
    public static void Write(SourceCodeWriter sourceCodeWriter, TypedServiceProviderModel serviceProviderModel,
        IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencyDictionary,
        IDictionary<ServiceModelCollection.ServiceKey, List<DecoratorModel>>? decorators = null)
    {
        sourceCodeWriter.WriteLine(
            $"public partial class ServiceRegistrar_ : global::Inject.NET.Services.ServiceRegistrar<{serviceProviderModel.Prefix}ServiceProvider_, {serviceProviderModel.Prefix}ServiceProvider_>");

        sourceCodeWriter.WriteLine("{");

        sourceCodeWriter.WriteLine("public ServiceRegistrar_()");
        sourceCodeWriter.WriteLine("{");

        WriteRegistration(sourceCodeWriter, dependencyDictionary, decorators, string.Empty);

        // Call user-defined configuration hook for extension method registrations
        sourceCodeWriter.WriteLine("ConfigureServices();");

        sourceCodeWriter.WriteLine("}");

        sourceCodeWriter.WriteLine();

        // Declare partial method that users can implement for extension-based service registration
        sourceCodeWriter.WriteLine("partial void ConfigureServices();");

        sourceCodeWriter.WriteLine();

        sourceCodeWriter.WriteLine($$"""
                                     public override async ValueTask<{{serviceProviderModel.Prefix}}ServiceProvider_> BuildAsync({{serviceProviderModel.Prefix}}ServiceProvider_ parent)
                                     {
                                         var serviceProvider = new {{serviceProviderModel.Prefix}}ServiceProvider_(ServiceFactoryBuilders.AsReadOnly());
                                         
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

    private static void WriteRegistration(SourceCodeWriter sourceCodeWriter, IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencyDictionary, IDictionary<ServiceModelCollection.ServiceKey, List<DecoratorModel>>? decorators, string prefix)
    {
        foreach (var (_, serviceModels) in dependencyDictionary)
        {
            foreach (var serviceModel in serviceModels.Where(x => !x.ResolvedFromParent))
            {
                WriteRegistration(sourceCodeWriter, dependencyDictionary, decorators, prefix, serviceModel);
            }
        }
    }

    private static void WriteRegistration(SourceCodeWriter sourceCodeWriter, IDictionary<ServiceModelCollection.ServiceKey, List<ServiceModel>> dependencyDictionary, IDictionary<ServiceModelCollection.ServiceKey, List<DecoratorModel>>? decorators, string prefix,
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
            baseInvocation = $"new {serviceModel.ImplementationType.GloballyQualified()}({string.Join(", ", BuildParameters(serviceModel))})";
        }

        // Check if there are decorators for this service
        string finalInvocation;
        if (decorators != null && decorators.TryGetValue(serviceModel.ServiceKey, out var decoratorList) && decoratorList.Count > 0)
        {
            // Wrap the base implementation with decorators
            var wrappedInvocation = baseInvocation;

            foreach (var decorator in decoratorList)
            {
                wrappedInvocation = WrapWithDecorator(decorator, wrappedInvocation);
            }

            finalInvocation = wrappedInvocation;
        }
        else
        {
            finalInvocation = baseInvocation;
        }

        // Check if method injection is needed
        if (MethodInjectionHelper.HasInjectMethods(serviceModel))
        {
            sourceCodeWriter.WriteLine("Factory = (scope, type, key) =>");
            sourceCodeWriter.WriteLine("{");
            sourceCodeWriter.WriteLine($"var __instance = {finalInvocation};");

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
            sourceCodeWriter.WriteLine(finalInvocation);
        }

        sourceCodeWriter.WriteLine("});");
        sourceCodeWriter.WriteLine();
    }
    
    private static IEnumerable<string> BuildParameters(ServiceModel serviceModel)
    {
        foreach (var serviceModelParameter in serviceModel.Parameters)
        {
            // Handle Lazy<T> parameters
            if (serviceModelParameter.IsLazy && serviceModelParameter.LazyInnerType != null)
            {
                var innerType = serviceModelParameter.LazyInnerType;
                if (serviceModelParameter.Key is null)
                {
                    yield return $"new global::System.Lazy<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>())";
                }
                else
                {
                    yield return $"new global::System.Lazy<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>(\"{serviceModelParameter.Key}\"))";
                }
            }
            // Handle Func<T> parameters - wrap service resolution in a lambda
            else if (serviceModelParameter.IsFunc && serviceModelParameter.FuncInnerType != null)
            {
                var innerType = serviceModelParameter.FuncInnerType;
                yield return $"new global::System.Func<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>())";
            }
            // Handle enumerable parameters - extract element type and call GetServices
            else if (serviceModelParameter.IsEnumerable)
            {
                // Extract element type from IEnumerable<T> or IReadOnlyList<T>
                var elementType = serviceModelParameter.Type is Microsoft.CodeAnalysis.INamedTypeSymbol { IsGenericType: true } genericType
                    ? genericType.TypeArguments[0]
                    : serviceModelParameter.Type;

                var key = serviceModelParameter.Key is null ? "null" : $"\"{serviceModelParameter.Key}\"";

                // Use collection literal syntax [..] to support both IEnumerable<T> and IReadOnlyList<T>
                yield return $"[..scope.GetServices<{elementType.GloballyQualified()}>({key})]";
            }
            // Handle optional parameters (with default values)
            else if (serviceModelParameter.IsOptional)
            {
                yield return $"scope.GetOptionalService<{serviceModelParameter.Type.GloballyQualified()}>() ?? {serviceModelParameter.DefaultValue ?? "default"}";
            }
            // Handle nullable parameters
            else if (serviceModelParameter.IsNullable)
            {
                yield return $"scope.GetOptionalService<{serviceModelParameter.Type.GloballyQualified()}>()";
            }
            // Handle required parameters
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
            // Handle Lazy<T> parameters in open generic contexts
            if (serviceModelParameter.IsLazy && serviceModelParameter.LazyInnerType != null)
            {
                var innerType = serviceModelParameter.LazyInnerType;
                if (serviceModelParameter.Key is null)
                {
                    yield return $"new global::System.Lazy<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>())";
                }
                else
                {
                    yield return $"new global::System.Lazy<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>(\"{serviceModelParameter.Key}\"))";
                }
            }
            // Handle Func<T> parameters
            else if (serviceModelParameter.IsFunc && serviceModelParameter.FuncInnerType != null)
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
                    // Handle optional/nullable parameters
                    if (serviceModelParameter.IsOptional)
                    {
                        yield return $"scope.GetOptionalService<{serviceModelParameter.Type.GloballyQualified()}>() ?? {serviceModelParameter.DefaultValue ?? "default"}";
                    }
                    else if (serviceModelParameter.IsNullable)
                    {
                        yield return $"scope.GetOptionalService<{serviceModelParameter.Type.GloballyQualified()}>()";
                    }
                    else
                    {
                        yield return $"scope.GetRequiredService<{serviceModelParameter.Type.GloballyQualified()}>()";
                    }
                }
            }
            else
            {
                // Non-generic parameter, resolve normally with optional/nullable handling
                if (serviceModelParameter.IsOptional)
                {
                    yield return $"scope.GetOptionalService<{serviceModelParameter.Type.GloballyQualified()}>() ?? {serviceModelParameter.DefaultValue ?? "default"}";
                }
                else if (serviceModelParameter.IsNullable)
                {
                    yield return $"scope.GetOptionalService<{serviceModelParameter.Type.GloballyQualified()}>()";
                }
                else
                {
                    yield return $"scope.GetRequiredService<{serviceModelParameter.Type.GloballyQualified()}>()";
                }
            }
        }
    }

    private static string WrapWithDecorator(DecoratorModel decorator, string innerInvocation)
    {
        // Build decorator constructor invocation
        var decoratorParams = new List<string>();

        foreach (var param in decorator.Parameters)
        {
            // Check if this parameter is the decorated service
            if (Microsoft.CodeAnalysis.SymbolEqualityComparer.Default.Equals(param.Type, decorator.ServiceType))
            {
                // This is the inner service parameter
                decoratorParams.Add(innerInvocation);
            }
            else if (param.IsLazy && param.LazyInnerType != null)
            {
                // Handle Lazy<T> parameters
                var innerType = param.LazyInnerType;
                if (param.Key is null)
                {
                    decoratorParams.Add($"new global::System.Lazy<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>())");
                }
                else
                {
                    decoratorParams.Add($"new global::System.Lazy<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>(\"{param.Key}\"))");
                }
            }
            else
            {
                // This is another dependency - resolve it from scope
                if (param.IsFunc && param.FuncInnerType != null)
                {
                    var innerType = param.FuncInnerType;
                    decoratorParams.Add($"new global::System.Func<{innerType.GloballyQualified()}>(() => scope.GetRequiredService<{innerType.GloballyQualified()}>())");
                }
                else if (param.IsEnumerable)
                {
                    // Handle enumerable parameters
                    var elementType = param.Type is Microsoft.CodeAnalysis.INamedTypeSymbol { IsGenericType: true } genericType
                        ? genericType.TypeArguments[0]
                        : param.Type;

                    var key = param.Key is null ? "null" : $"\"{param.Key}\"";
                    decoratorParams.Add($"[..scope.GetServices<{elementType.GloballyQualified()}>({key})]");
                }
                else if (param.IsOptional)
                {
                    decoratorParams.Add($"scope.GetOptionalService<{param.Type.GloballyQualified()}>() ?? {param.DefaultValue ?? "default"}");
                }
                else if (param.IsNullable)
                {
                    decoratorParams.Add($"scope.GetOptionalService<{param.Type.GloballyQualified()}>()");
                }
                else
                {
                    decoratorParams.Add($"scope.GetRequiredService<{param.Type.GloballyQualified()}>()");
                }
            }
        }

        return $"new {decorator.DecoratorType.GloballyQualified()}({string.Join(", ", decoratorParams)})";
    }
}