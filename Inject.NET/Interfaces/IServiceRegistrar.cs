using System.Diagnostics.CodeAnalysis;
using Inject.NET.Delegates;
using Inject.NET.Enums;
using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface IServiceRegistrar
{
    ServiceFactoryBuilders ServiceFactoryBuilders { get; }
    
    IServiceRegistrar Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Func<IServiceScope, Type, T> factory, Lifetime lifetime);
    
    IServiceRegistrar RegisterOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime);
    
    IServiceRegistrar RegisterKeyed<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Func<IServiceScope, Type, T> factory, Lifetime lifetime, string key);
    
    IServiceRegistrar RegisterKeyedOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime, string key);

    
    OnBeforeBuild OnBeforeBuild { get; set; }
    
    Task<IServiceProvider> BuildAsync(IServiceProvider rootServiceProvider);
}