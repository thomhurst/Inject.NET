using Inject.NET.Delegates;
using Inject.NET.Enums;
using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface ITenantedServiceRegistrar
{
    ServiceFactoryBuilders ServiceFactoryBuilders { get; }
    
    ITenantedServiceRegistrar Register<T>(Func<IServiceScope, Type, T> factory, Lifetime lifetime);
    
    ITenantedServiceRegistrar RegisterOpenGeneric(Type serviceType, Type implementationType, Lifetime lifetime);

    ITenantedServiceRegistrar RegisterKeyed<T>(Func<IServiceScope, Type, string, T> factory, Lifetime lifetime, string key);
    
    ITenantedServiceRegistrar RegisterKeyedOpenGeneric(Type serviceType, Type implementationType, Lifetime lifetime, string key);

    OnBeforeTenantBuild OnBeforeBuild { get; set; }
    
    Task<ITenantedServiceProvider> BuildAsync();
    
    IServiceRegistrar GetOrCreateTenant(string tenantId);
}