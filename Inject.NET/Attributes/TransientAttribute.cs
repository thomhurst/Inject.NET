using Inject.NET.Enums;

namespace Inject.NET.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TransientAttribute : DependencyInjectionAttribute 
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
public sealed class TransientAttribute<TImplementation> : DependencyInjectionAttribute<TImplementation, TImplementation> 
    where TImplementation : class
{
    public override Lifetime Lifetime => Lifetime.Transient;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class TransientAttribute<TService, TImplementation> : DependencyInjectionAttribute<TService, TImplementation> 
    where TService : class 
    where TImplementation : class, TService
{
    public override Lifetime Lifetime => Lifetime.Transient;
}