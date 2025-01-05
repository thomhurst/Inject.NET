using Inject.NET.Delegates;
using Inject.NET.Enums;
using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface IServiceRegistrar
{
    ServiceFactoryBuilders ServiceFactoryBuilders { get; }
    
    IServiceRegistrar Register<T>(Func<IServiceScope, Type, T> factory, Lifetime lifetime);
    
    IServiceRegistrar RegisterKeyed<T>(Func<IServiceScope, Type, string, T> factory, Lifetime lifetime, string key);
    
    OnBeforeBuild OnBeforeBuild { get; set; }
    
    Task<IServiceProvider> BuildAsync(IServiceProvider defaultServiceProvider);
}

public interface ITenantedServiceRegistrar
{
    ServiceFactoryBuilders ServiceFactoryBuilders { get; }
    
    ITenantedServiceRegistrar Register<T>(Func<IServiceScope, Type, T> factory, Lifetime lifetime);
    
    ITenantedServiceRegistrar RegisterKeyed<T>(Func<IServiceScope, Type, string, T> factory, Lifetime lifetime, string key);
    
    OnBeforeTenantBuild OnBeforeBuild { get; set; }
    
    Task<ITenantedServiceProvider> BuildAsync();
    
    IServiceRegistrar GetOrCreateTenant(string tenantId);
}