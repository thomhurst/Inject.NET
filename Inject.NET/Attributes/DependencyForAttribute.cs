using Inject.NET.Enums;

namespace Inject.NET.Attributes;

public abstract class DependencyForAttribute<TService, TImplementation>() : DependencyForAttribute(typeof(TService), typeof(TImplementation))
    where TService : class
    where TImplementation : class, TService;

public abstract class DependencyForAttribute : Attribute
{
    internal DependencyForAttribute(Type serviceType, Type implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }

    public abstract Lifetime Lifetime { get; }

    public Type ServiceType { get; }
    public Type ImplementationType { get; }
}