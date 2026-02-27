using System.Reflection;
using Inject.NET.Enums;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using Inject.NET.Services;

namespace Inject.NET;

/// <summary>
/// Provides a fluent API for scanning assemblies and registering discovered services
/// with the dependency injection container based on conventions.
/// </summary>
/// <remarks>
/// Assembly scanning enables convention-based registration where services are discovered
/// at runtime by examining types in specified assemblies. This is useful for large projects
/// where manually registering each service would be tedious.
/// </remarks>
public class AssemblyScanner
{
    private static readonly Type ServiceFactoryOpenGenericType = typeof(ServiceFactory<>);

    private readonly List<Assembly> _assemblies = [];
    private readonly List<Type> _serviceTypes = [];
    private bool _useDefaultConventions;
    private Lifetime _lifetime = Lifetime.Transient;

    /// <summary>
    /// Adds an assembly to be scanned for service implementations.
    /// Multiple assemblies can be added by calling this method multiple times.
    /// </summary>
    /// <param name="assembly">The assembly to scan</param>
    /// <returns>The scanner for fluent chaining</returns>
    public AssemblyScanner FromAssembly(Assembly assembly)
    {
        _assemblies.Add(assembly);
        return this;
    }

    /// <summary>
    /// Adds the assembly containing the specified type to be scanned for service implementations.
    /// This is a convenience method that avoids the need to reference assemblies directly.
    /// </summary>
    /// <typeparam name="T">A type whose containing assembly should be scanned</typeparam>
    /// <returns>The scanner for fluent chaining</returns>
    /// <example>
    /// <code>
    /// scanner.FromAssemblyOf&lt;MyService&gt;();
    /// </code>
    /// </example>
    public AssemblyScanner FromAssemblyOf<T>()
    {
        return FromAssembly(typeof(T).Assembly);
    }

    /// <summary>
    /// Registers all concrete implementations of the specified service type found in the scanned assemblies.
    /// Abstract classes and interfaces are excluded from registration.
    /// </summary>
    /// <typeparam name="TService">The service type (interface or base class) to find implementations of</typeparam>
    /// <returns>The scanner for fluent chaining</returns>
    /// <example>
    /// <code>
    /// scanner.AddAllTypesOf&lt;ICommandHandler&gt;();
    /// </code>
    /// </example>
    public AssemblyScanner AddAllTypesOf<TService>()
    {
        return AddAllTypesOf(typeof(TService));
    }

    /// <summary>
    /// Registers all concrete implementations of the specified service type found in the scanned assemblies.
    /// Abstract classes and interfaces are excluded from registration.
    /// </summary>
    /// <param name="serviceType">The service type (interface or base class) to find implementations of</param>
    /// <returns>The scanner for fluent chaining</returns>
    public AssemblyScanner AddAllTypesOf(Type serviceType)
    {
        _serviceTypes.Add(serviceType);
        return this;
    }

    /// <summary>
    /// Enables default naming convention matching where an interface IFoo is matched
    /// to a class named Foo in the scanned assemblies. Only concrete, non-abstract classes
    /// are considered. Interfaces without a matching implementation are silently skipped.
    /// </summary>
    /// <returns>The scanner for fluent chaining</returns>
    /// <example>
    /// <code>
    /// scanner.WithDefaultConventions(); // IFoo -> Foo, IBarService -> BarService
    /// </code>
    /// </example>
    public AssemblyScanner WithDefaultConventions()
    {
        _useDefaultConventions = true;
        return this;
    }

    /// <summary>
    /// Sets the lifetime for all services discovered by this scanner to Singleton.
    /// </summary>
    /// <returns>The scanner for fluent chaining</returns>
    public AssemblyScanner AsSingleton()
    {
        _lifetime = Lifetime.Singleton;
        return this;
    }

    /// <summary>
    /// Sets the lifetime for all services discovered by this scanner to Scoped.
    /// </summary>
    /// <returns>The scanner for fluent chaining</returns>
    public AssemblyScanner AsScoped()
    {
        _lifetime = Lifetime.Scoped;
        return this;
    }

    /// <summary>
    /// Sets the lifetime for all services discovered by this scanner to Transient.
    /// This is the default lifetime if none is specified.
    /// </summary>
    /// <returns>The scanner for fluent chaining</returns>
    public AssemblyScanner AsTransient()
    {
        _lifetime = Lifetime.Transient;
        return this;
    }

    /// <summary>
    /// Applies the scanner configuration to the service registrar, discovering and registering
    /// all matching types from the scanned assemblies.
    /// </summary>
    /// <param name="registrar">The service registrar to register discovered services with</param>
    internal void Apply(IServiceRegistrar registrar)
    {
        var candidateTypes = GetCandidateTypes();

        foreach (var serviceType in _serviceTypes)
        {
            RegisterAllTypesOf(registrar, serviceType, candidateTypes);
        }

        if (_useDefaultConventions)
        {
            ApplyDefaultConventions(registrar, candidateTypes);
        }
    }

    private List<Type> GetCandidateTypes()
    {
        var types = new List<Type>();

        foreach (var assembly in _assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type is { IsClass: true, IsAbstract: false }
                    && !type.IsGenericTypeDefinition
                    && !type.ContainsGenericParameters)
                {
                    types.Add(type);
                }
            }
        }

        return types;
    }

    private void RegisterAllTypesOf(IServiceRegistrar registrar, Type serviceType, List<Type> candidateTypes)
    {
        foreach (var implementationType in candidateTypes)
        {
            if (serviceType.IsAssignableFrom(implementationType))
            {
                RegisterService(registrar, serviceType, implementationType);
            }
        }
    }

    private void ApplyDefaultConventions(IServiceRegistrar registrar, List<Type> candidateTypes)
    {
        foreach (var candidateType in candidateTypes)
        {
            foreach (var interfaceType in candidateType.GetInterfaces())
            {
                if (!interfaceType.IsGenericTypeDefinition
                    && !interfaceType.ContainsGenericParameters
                    && MatchesDefaultConvention(interfaceType, candidateType))
                {
                    RegisterService(registrar, interfaceType, candidateType);
                }
            }
        }
    }

    private void RegisterService(IServiceRegistrar registrar, Type serviceType, Type implementationType)
    {
        var factory = CreateFactory(implementationType);

        registrar.Register(new ServiceDescriptor
        {
            ServiceType = serviceType,
            ImplementationType = implementationType,
            Lifetime = _lifetime,
            Factory = factory
        });
    }

    /// <summary>
    /// Creates a factory delegate for the specified implementation type by invoking
    /// the generic <see cref="ServiceFactory{T}.Create"/> method via reflection.
    /// The underlying factory uses compiled expression trees for optimal performance.
    /// </summary>
    private static Func<IServiceScope, Type, string?, object> CreateFactory(Type implementationType)
    {
        var closedFactoryType = ServiceFactoryOpenGenericType.MakeGenericType(implementationType);

        var createMethod = closedFactoryType.GetMethod(
            nameof(ServiceFactory<object>.Create),
            BindingFlags.Public | BindingFlags.Static)!;

        return (Func<IServiceScope, Type, string?, object>)Delegate.CreateDelegate(
            typeof(Func<IServiceScope, Type, string?, object>),
            createMethod);
    }

    /// <summary>
    /// Determines whether a type name matches the default convention for an interface name.
    /// The convention is that interface IFoo should be implemented by class Foo.
    /// </summary>
    /// <param name="interfaceType">The interface type</param>
    /// <param name="implementationType">The candidate implementation type</param>
    /// <returns>True if the types match the IFoo/Foo naming convention</returns>
    public static bool MatchesDefaultConvention(Type interfaceType, Type implementationType)
    {
        var interfaceName = interfaceType.Name;

        // Interface must start with 'I' followed by an uppercase letter
        if (interfaceName.Length < 2 || interfaceName[0] != 'I' || !char.IsUpper(interfaceName[1]))
        {
            return false;
        }

        // Strip the 'I' prefix to get the expected implementation name
        var expectedName = interfaceName.Substring(1);

        return implementationType.Name == expectedName;
    }
}
