using System.Collections.Concurrent;
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
    /// partial void ConfigureServices()
    /// {
    ///     this.AddServiceCollection(services =>
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

        foreach (var descriptor in services)
        {
            registrar.Register(ConvertDescriptor(descriptor));
        }

        // Register MEDI infrastructure adapters if not already present
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

        // Instance-based descriptor
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
                ExternallyOwned = true,
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
                return ActivatorUtilities.CreateInstance(scope, closedImplType);
            };
        }

        // Closed type: cache an ObjectFactory for fast repeated creation.
        var objectFactory = FactoryCache.GetOrAdd(
            (serviceType, implementationType),
            static tuple => ActivatorUtilities.CreateFactory(tuple.implementationType, Type.EmptyTypes));

        return (scope, type, key) => objectFactory(scope, null);
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
    /// Inject.NET uses string keys while MEDI uses object keys. Non-string keys are converted
    /// using <see cref="object.ToString"/>.
    /// </summary>
    private static string? ConvertKey(object? key) => key switch
    {
        null => null,
        string s => s,
        _ => key.ToString()
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
