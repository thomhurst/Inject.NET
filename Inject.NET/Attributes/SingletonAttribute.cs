using Inject.NET.Enums;

namespace Inject.NET.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SingletonAttribute<TImplementation> : DependencyForAttribute<TImplementation, TImplementation> 
    where TImplementation : class
{
    public override Lifetime Lifetime => Lifetime.Singleton;
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SingletonAttribute<TService, TImplementation> : DependencyForAttribute<TService, TImplementation> 
    where TService : class 
    where TImplementation : class, TService
{
    public override Lifetime Lifetime => Lifetime.Singleton;
}