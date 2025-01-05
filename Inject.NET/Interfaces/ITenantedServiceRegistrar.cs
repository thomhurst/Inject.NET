using System.Diagnostics.CodeAnalysis;
using Inject.NET.Delegates;
using Inject.NET.Enums;
using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface ITenantedServiceRegistrar
{
    ServiceFactoryBuilders ServiceFactoryBuilders { get; }
    
    ITenantedServiceRegistrar Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Func<IServiceScope, Type, T> factory, Lifetime lifetime);
    
    ITenantedServiceRegistrar RegisterOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime);

    ITenantedServiceRegistrar RegisterKeyed<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Func<IServiceScope, Type, string, T> factory, Lifetime lifetime, string key);
    
    ITenantedServiceRegistrar RegisterKeyedOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime, string key);

    OnBeforeTenantBuild OnBeforeBuild { get; set; }
    
    Task<ITenantedServiceProvider> BuildAsync();
    
    IServiceRegistrar GetOrCreateTenant(string tenantId);
}