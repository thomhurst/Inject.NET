using Inject.NET.Delegates;
using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface ITenantedServiceRegistrar
{
    ServiceFactoryBuilders ServiceFactoryBuilders { get; }
    
    ITenantedServiceRegistrar Register(ServiceDescriptor descriptor);
    
    OnBeforeTenantBuild OnBeforeBuild { get; set; }
    
    ValueTask<IServiceProviderRoot> BuildAsync();
    
    IServiceRegistrar GetOrCreateTenant(string tenantId);
}