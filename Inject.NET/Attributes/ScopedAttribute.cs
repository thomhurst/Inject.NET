using Inject.NET.Enums;

namespace Inject.NET.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ScopedAttribute : DependencyForAttribute 
{
    public ScopedAttribute(Type implementationType) : base(implementationType, implementationType)
    {
    }
    
    public ScopedAttribute(Type serviceType, Type implementationType) : base(serviceType, implementationType)
    {
    }

    public override Lifetime Lifetime => Lifetime.Scoped;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ScopedAttribute<TImplementation> : DependencyForAttribute<TImplementation, TImplementation> 
    where TImplementation : class
{
    public override Lifetime Lifetime => Lifetime.Scoped;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ScopedAttribute<TService, TImplementation> : DependencyForAttribute<TService, TImplementation> 
    where TService : class 
    where TImplementation : class, TService
{
    public override Lifetime Lifetime => Lifetime.Scoped;
}