using Inject.NET.Enums;

namespace Inject.NET.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TransientAttribute : DependencyForAttribute 
{
    public TransientAttribute(Type implementationType) : base(implementationType, implementationType)
    {
    }
    
    public TransientAttribute(Type serviceType, Type implementationType) : base(serviceType, implementationType)
    {
    }

    public override Lifetime Lifetime => Lifetime.Transient;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TransientAttribute<TImplementation> : DependencyForAttribute<TImplementation, TImplementation> 
    where TImplementation : class
{
    public override Lifetime Lifetime => Lifetime.Transient;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TransientAttribute<TService, TImplementation> : DependencyForAttribute<TService, TImplementation> 
    where TService : class 
    where TImplementation : class, TService
{
    public override Lifetime Lifetime => Lifetime.Transient;
}