using System.Collections.Concurrent;
using System.Reflection;
using Inject.NET.Enums;
using Inject.NET.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using InjectDescriptor = Inject.NET.Models.ServiceDescriptor;
using MsDescriptor = Microsoft.Extensions.DependencyInjection.ServiceDescriptor;

namespace Inject.NET.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for replaying Microsoft.Extensions.DependencyInjection service registrations
/// into an Inject.NET <see cref="IServiceRegistrar"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Cache of <see cref="ObjectFactory"/> instances keyed by (serviceType, implementationType).
    /// <see cref="ActivatorUtilities.CreateFactory"/> is expensive, so we cache per type pair.
    /// </summary>
    private static readonly ConcurrentDictionary<(Type serviceType, Type implementationType), ObjectFactory> FactoryCache = new();

    /// <summary>
    /// Cache of constructors sorted by parameter count descending, keyed by implementation type.
    /// Reflection + sort is performed once per type instead of on every greedy-fallback invocation.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, ConstructorInfo[]> SortedConstructorsCache = new();

    /// <summary>
    /// Cache of the constructor that last resolved successfully for a given implementation type.
    /// On subsequent calls the greedy fallback tries the winning constructor first, avoiding a
    /// full search of all constructors. Falls through to the full search if the cached constructor
    /// can no longer be satisfied (e.g. because resolution context differs).
    /// </summary>
    private static readonly ConcurrentDictionary<Type, ConstructorInfo> WinningConstructorCache = new();

    /// <summary>
    /// Replays all service registrations from a Microsoft.Extensions.DependencyInjection
    /// <see cref="IServiceCollection"/> into the Inject.NET <see cref="IServiceRegistrar"/>.
    /// Also registers MEDI infrastructure adapters (<see cref="IServiceScopeFactory"/>,
    /// <see cref="IServiceProviderIsService"/>) if not already present.
    /// </summary>
    /// <param name="registrar">The Inject.NET service registrar to add services to.</param>
    /// <param name="configure">
    /// An action that configures an <see cref="IServiceCollection"/>. All registrations made
    /// inside this action will be converted and replayed into the Inject.NET registrar.
    /// </param>
    /// <returns>The registrar for fluent chaining.</returns>
    /// <example>
    /// <code>
    /// static partial void ConfigureServices(IServiceRegistrar registrar)
    /// {
    ///     registrar.AddServiceCollection(services =>
    ///     {
    ///         services.AddOptions&lt;MyOptions&gt;().Configure(o => o.Value = "configured");
    ///         services.AddLogging();
    ///     });
    /// }
    /// </code>
    /// </example>
    public static IServiceRegistrar AddServiceCollection(
        this IServiceRegistrar registrar,
        Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return registrar.AddServiceCollection(services);
    }

    /// <summary>
    /// Replays all service registrations from an existing <see cref="IServiceCollection"/>
    /// into the Inject.NET <see cref="IServiceRegistrar"/>.
    /// Also registers MEDI infrastructure adapters (<see cref="IServiceScopeFactory"/>,
    /// <see cref="IServiceProviderIsService"/>) if not already present.
    /// </summary>
    /// <param name="registrar">The Inject.NET service registrar to add services to.</param>
    /// <param name="services">The service collection whose registrations will be replayed.</param>
    /// <returns>The registrar for fluent chaining.</returns>
    public static IServiceRegistrar AddServiceCollection(
        this IServiceRegistrar registrar,
        IServiceCollection services)
    {
        foreach (var descriptor in services)
        {
            registrar.Register(ConvertDescriptor(descriptor));
        }

        RegisterAdaptersIfMissing(registrar);

        return registrar;
    }

    /// <summary>
    /// Converts a MEDI <see cref="MsDescriptor"/> to an Inject.NET <see cref="InjectDescriptor"/>.
    /// Handles the three MEDI descriptor shapes: instance-based, factory-based, and type-based.
    /// </summary>
    private static InjectDescriptor ConvertDescriptor(MsDescriptor descriptor)
    {
        var isKeyed = descriptor.IsKeyedService;
        var serviceType = descriptor.ServiceType;
        var lifetime = ConvertLifetime(descriptor.Lifetime);
        var key = isKeyed ? ConvertKey(descriptor.ServiceKey) : null;

        // Determine the implementation type for the Inject.NET descriptor.
        // For instance or factory registrations where no implementation type is specified,
        // fall back to the service type itself.
        var implementationType = isKeyed
            ? descriptor.KeyedImplementationType ?? serviceType
            : descriptor.ImplementationType ?? serviceType;

        // Instance-based descriptor.
        // MEDI semantics: AddSingleton(instance) means the CONTAINER owns the instance and
        // disposes it at root shutdown, so ExternallyOwned must be false to match.
        if (isKeyed ? descriptor.KeyedImplementationInstance is not null : descriptor.ImplementationInstance is not null)
        {
            var instance = isKeyed
                ? descriptor.KeyedImplementationInstance!
                : descriptor.ImplementationInstance!;

            return new InjectDescriptor
            {
                ServiceType = serviceType,
                ImplementationType = instance.GetType(),
                Lifetime = lifetime,
                Key = key,
                ExternallyOwned = false,
                Factory = (scope, type, k) => instance
            };
        }

        // Factory-based descriptor
        if (isKeyed ? descriptor.KeyedImplementationFactory is not null : descriptor.ImplementationFactory is not null)
        {
            if (isKeyed)
            {
                var factory = descriptor.KeyedImplementationFactory!;
                return new InjectDescriptor
                {
                    ServiceType = serviceType,
                    ImplementationType = implementationType,
                    Lifetime = lifetime,
                    Key = key,
                    Factory = (scope, type, k) => factory(scope, k)
                };
            }
            else
            {
                var factory = descriptor.ImplementationFactory!;
                return new InjectDescriptor
                {
                    ServiceType = serviceType,
                    ImplementationType = implementationType,
                    Lifetime = lifetime,
                    Key = key,
                    Factory = (scope, type, k) => factory(scope)
                };
            }
        }

        // Type-based descriptor
        var implType = isKeyed
            ? descriptor.KeyedImplementationType!
            : descriptor.ImplementationType!;

        return new InjectDescriptor
        {
            ServiceType = serviceType,
            ImplementationType = implType,
            Lifetime = lifetime,
            Key = key,
            Factory = CreateTypeFactory(serviceType, implType)
        };
    }

    /// <summary>
    /// Creates a factory delegate for type-based registrations using <see cref="ActivatorUtilities"/>.
    /// For closed (non-generic) types, uses a cached <see cref="ObjectFactory"/> from
    /// <see cref="ActivatorUtilities.CreateFactory"/> for optimal performance.
    /// For open generic types, falls back to <see cref="ActivatorUtilities.CreateInstance"/>
    /// at resolution time since the concrete type is not known until the closed generic is requested.
    /// </summary>
    private static Func<Inject.NET.Interfaces.IServiceScope, Type, string?, object> CreateTypeFactory(Type serviceType, Type implementationType)
    {
        if (implementationType.IsGenericTypeDefinition)
        {
            // Open generic: must create instance at resolution time with the runtime-closed type.
            // The 'type' parameter will be the closed generic service type requested at resolution.
            return (scope, type, key) =>
            {
                var closedImplType = implementationType.MakeGenericType(type.GetGenericArguments());
                // Use GetResolutionScope to ensure we can resolve all lifetimes,
                // not just singletons (which is what the singleton scope provides).
                var resolutionScope = GetResolutionScope(scope);
                return CreateInstanceGreedy(resolutionScope, closedImplType);
            };
        }

        // Closed type: try to cache an ObjectFactory for fast repeated creation.
        // Falls back to greedy constructor resolution only for types whose public
        // constructors are genuinely ambiguous for ActivatorUtilities (multiple public
        // ctors with no clear preferred one, e.g. ConsoleLifetime). All other errors
        // must propagate so legitimate registration mistakes are not silently hidden.
        var objectFactory = FactoryCache.GetOrAdd(
            (serviceType, implementationType),
            static tuple =>
            {
                if (tuple.implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length > 1)
                {
                    try
                    {
                        return ActivatorUtilities.CreateFactory(tuple.implementationType, Type.EmptyTypes);
                    }
                    catch (InvalidOperationException)
                    {
                        // Ambiguous public constructors - fall back to greedy resolution at runtime.
                        return null!;
                    }
                }

                return ActivatorUtilities.CreateFactory(tuple.implementationType, Type.EmptyTypes);
            });

        if (objectFactory is not null)
        {
            return (scope, type, key) => objectFactory(scope, null);
        }

        // Greedy fallback for multi-constructor types
        return (scope, type, key) =>
        {
            var resolutionScope = GetResolutionScope(scope);
            return CreateInstanceGreedy(resolutionScope, implementationType);
        };
    }

    /// <summary>
    /// Returns a scope suitable for greedy constructor resolution. When the provided
    /// scope is a singleton scope (which by itself can only resolve singletons), a
    /// regular scope is created from the underlying provider so transient and scoped
    /// dependencies can also be resolved.
    /// <para>
    /// The created scope is NOT disposed here — it leaks for the lifetime of the
    /// provider. This is an intentional tradeoff: disposing the throwaway scope after
    /// constructing the instance would dispose any transient/scoped dependencies the
    /// instance captured, including IConfigureOptions&lt;T&gt; that Options chains rely on.
    /// The leak is bounded because singletons go through this path once per type, and
    /// in practice only a handful of MEDI infrastructure types (OptionsManager, etc.)
    /// need this path.
    /// </para>
    /// </summary>
    private static Inject.NET.Interfaces.IServiceScope GetResolutionScope(Inject.NET.Interfaces.IServiceScope scope)
    {
        if (scope is Inject.NET.Interfaces.ISingleton)
        {
            // The singleton scope can't resolve non-singleton services. Ask the underlying
            // provider for a scope capable of resolving all lifetimes.
            var provider = scope.GetService(typeof(Inject.NET.Interfaces.IServiceProvider)) as Inject.NET.Interfaces.IServiceProvider;
            if (provider != null)
            {
                return provider.CreateScope();
            }
        }

        return scope;
    }

    /// <summary>
    /// Creates an instance of the specified type using greedy constructor selection.
    /// Picks the constructor with the most parameters that can all be resolved from the provider.
    /// Falls back to the constructor with fewer parameters if the greediest cannot be satisfied.
    /// This avoids the ambiguous-constructor error that <see cref="ActivatorUtilities.CreateInstance"/>
    /// throws when multiple constructors match.
    /// <para>
    /// Uses <see cref="SortedConstructorsCache"/> to avoid re-reflecting on every call, and
    /// <see cref="WinningConstructorCache"/> to fast-path to the constructor that succeeded
    /// previously for this type.
    /// </para>
    /// </summary>
    private static object CreateInstanceGreedy(System.IServiceProvider provider, Type instanceType)
    {
        var constructors = SortedConstructorsCache.GetOrAdd(instanceType, static t =>
            t.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)
                .ToArray());

        if (constructors.Length == 0)
        {
            throw new InvalidOperationException(
                $"No public constructors found on type '{instanceType.FullName}'.");
        }

        // Fast path: try the constructor that previously won for this type.
        if (WinningConstructorCache.TryGetValue(instanceType, out var cachedCtor)
            && TryInvokeConstructor(provider, cachedCtor, out var cachedInstance))
        {
            return cachedInstance;
        }

        foreach (var ctor in constructors)
        {
            if (TryInvokeConstructor(provider, ctor, out var instance))
            {
                WinningConstructorCache[instanceType] = ctor;
                return instance;
            }
        }

        // Last resort: try ActivatorUtilities which may give a better error message
        return ActivatorUtilities.CreateInstance(provider, instanceType);
    }

    /// <summary>
    /// Attempts to resolve all parameters of <paramref name="ctor"/> from the provider and
    /// invoke it. Returns <c>false</c> if any required parameter cannot be resolved.
    /// </summary>
    private static bool TryInvokeConstructor(
        System.IServiceProvider provider,
        ConstructorInfo ctor,
        out object instance)
    {
        var parameters = ctor.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            var service = ResolveParameter(provider, paramType);

            if (service == null && !parameters[i].HasDefaultValue)
            {
                instance = null!;
                return false;
            }

            args[i] = service ?? parameters[i].DefaultValue;
        }

        instance = ctor.Invoke(args);
        return true;
    }

    /// <summary>
    /// Resolves a constructor parameter from the service provider.
    /// For collection-shaped parameters (<c>IEnumerable&lt;T&gt;</c>, <c>T[]</c>,
    /// <c>IReadOnlyList&lt;T&gt;</c>, <c>IReadOnlyCollection&lt;T&gt;</c>,
    /// <c>ICollection&lt;T&gt;</c>, <c>IList&lt;T&gt;</c>), converts the
    /// <c>List&lt;object&gt;</c> returned by Inject.NET into a properly-typed array
    /// so that constructor invocation succeeds.
    /// </summary>
    private static object? ResolveParameter(System.IServiceProvider provider, Type paramType)
    {
        // Handle T[] array parameters: ask for IEnumerable<T> then convert.
        if (paramType.IsArray && paramType.GetArrayRank() == 1)
        {
            var elementType = paramType.GetElementType()!;
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            var items = provider.GetService(enumerableType);
            return items is null ? null : ConvertToTypedArray(items, elementType);
        }

        // Handle collection-shaped generic parameters (IReadOnlyList<T>, ICollection<T>, etc.)
        // by requesting IEnumerable<T> from the provider and converting the result to a
        // typed array — Inject.NET only resolves collection contents via IEnumerable<T>.
        if (paramType.IsGenericType)
        {
            var genericDef = paramType.GetGenericTypeDefinition();
            if (IsCollectionShape(genericDef))
            {
                var elementType = paramType.GetGenericArguments()[0];
                // For IEnumerable<T> itself, just ask directly.
                if (genericDef == typeof(IEnumerable<>))
                {
                    var enumerable = provider.GetService(paramType);
                    return enumerable is null ? null : ConvertToTypedArray(enumerable, elementType);
                }

                var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
                var items = provider.GetService(enumerableType);
                return items is null ? null : ConvertToTypedArray(items, elementType);
            }
        }

        return provider.GetService(paramType);
    }

    private static bool IsCollectionShape(Type genericDefinition)
    {
        return genericDefinition == typeof(IEnumerable<>)
            || genericDefinition == typeof(IReadOnlyList<>)
            || genericDefinition == typeof(IReadOnlyCollection<>)
            || genericDefinition == typeof(ICollection<>)
            || genericDefinition == typeof(IList<>);
    }

    private static object ConvertToTypedArray(object source, Type elementType)
    {
        if (source is System.Collections.IList list)
        {
            var typedArray = Array.CreateInstance(elementType, list.Count);
            for (var i = 0; i < list.Count; i++)
            {
                typedArray.SetValue(list[i], i);
            }
            return typedArray;
        }

        if (source is System.Collections.IEnumerable enumerable)
        {
            var buffer = new List<object?>();
            foreach (var item in enumerable)
            {
                buffer.Add(item);
            }
            var typedArray = Array.CreateInstance(elementType, buffer.Count);
            for (var i = 0; i < buffer.Count; i++)
            {
                typedArray.SetValue(buffer[i], i);
            }
            return typedArray;
        }

        return source;
    }

    /// <summary>
    /// Converts a MEDI <see cref="ServiceLifetime"/> to an Inject.NET <see cref="Lifetime"/>.
    /// </summary>
    private static Lifetime ConvertLifetime(ServiceLifetime lifetime) => lifetime switch
    {
        ServiceLifetime.Singleton => Lifetime.Singleton,
        ServiceLifetime.Scoped => Lifetime.Scoped,
        ServiceLifetime.Transient => Lifetime.Transient,
        _ => throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, $"Unknown ServiceLifetime value: {lifetime}")
    };

    /// <summary>
    /// Converts a MEDI service key (<see cref="object"/>?) to an Inject.NET key (<see cref="string"/>?).
    /// Inject.NET uses string keys while MEDI uses object keys. Non-string keys are rejected
    /// because falling back to <see cref="object.ToString"/> could silently collide distinct keys
    /// (e.g. enums, Guids with matching representations) and cause the wrong implementation to resolve.
    /// </summary>
    /// <exception cref="NotSupportedException">
    /// Thrown when a non-string service key is encountered. Replace non-string keys with stable
    /// string keys when registering services for replay into Inject.NET.
    /// </exception>
    private static string? ConvertKey(object? key) => key switch
    {
        null => null,
        string s => s,
        _ => throw new NotSupportedException(
            $"Inject.NET only supports string service keys, but a key of type '{key.GetType().FullName}' was encountered. " +
            $"Replace non-string keys with string keys when registering services for replay into Inject.NET.")
    };

    /// <summary>
    /// Registers the MEDI infrastructure adapters (<see cref="IServiceScopeFactory"/> and
    /// <see cref="IServiceProviderIsService"/>) as singletons if they are not already registered.
    /// These adapters allow MEDI-aware code to use scope creation and service-availability checks
    /// through the Inject.NET container.
    /// </summary>
    private static void RegisterAdaptersIfMissing(IServiceRegistrar registrar)
    {
        if (!registrar.ServiceFactoryBuilders.HasService(typeof(IServiceScopeFactory)))
        {
            registrar.Register(new InjectDescriptor
            {
                ServiceType = typeof(IServiceScopeFactory),
                ImplementationType = typeof(ServiceScopeFactoryAdapter),
                Lifetime = Lifetime.Singleton,
                Factory = (scope, type, key) => new ServiceScopeFactoryAdapter(
                    (Inject.NET.Interfaces.IServiceProvider)scope.GetService(typeof(Inject.NET.Interfaces.IServiceProvider))!)
            });
        }

        if (!registrar.ServiceFactoryBuilders.HasService(typeof(IServiceProviderIsService)))
        {
            registrar.Register(new InjectDescriptor
            {
                ServiceType = typeof(IServiceProviderIsService),
                ImplementationType = typeof(ServiceProviderIsServiceAdapter),
                Lifetime = Lifetime.Singleton,
                Factory = (scope, type, key) => new ServiceProviderIsServiceAdapter(
                    (Inject.NET.Interfaces.IServiceProvider)scope.GetService(typeof(Inject.NET.Interfaces.IServiceProvider))!)
            });
        }
    }
}
