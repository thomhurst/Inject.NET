using Inject.NET.Enums;

namespace Inject.NET.Attributes;

public abstract class DependencyInjectionAttribute<TService, TImplementation>() : DependencyInjectionAttribute(typeof(TService), typeof(TImplementation))
    where TService : class
    where TImplementation : class, TService;

public abstract class DependencyInjectionAttribute : Attribute, IDependencyInjectionAttribute
{
    internal DependencyInjectionAttribute(Type serviceType, Type implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }

    public abstract Lifetime Lifetime { get; }

    public string? Key { get; set; }

    /// <summary>
    /// When set to true, the container will not dispose this service when the scope or provider is disposed.
    /// Use this for services whose lifetime is managed externally (e.g., shared connections, pre-existing instances).
    /// </summary>
    public bool ExternallyOwned { get; set; }

    /// <summary>
    /// The name of a static factory method to use for creating instances of this service.
    /// By default, the method is looked up on the service provider class.
    /// Use <see cref="FactoryType"/> to specify a different class.
    /// </summary>
    public string? FactoryMethod { get; set; }

    /// <summary>
    /// The type containing the static factory method specified by <see cref="FactoryMethod"/>.
    /// When null, the factory method is looked up on the service provider class.
    /// </summary>
    public Type? FactoryType { get; set; }

    public Type ServiceType { get; }
    public Type ImplementationType { get; }
}