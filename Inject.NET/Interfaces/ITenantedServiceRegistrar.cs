using Inject.NET.Delegates;
using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface ITenantedServiceRegistrar<TServiceProviderRoot> where TServiceProviderRoot : IServiceProviderRoot
{
    ServiceFactoryBuilders ServiceFactoryBuilders { get; }
    
    ITenantedServiceRegistrar<TServiceProviderRoot> Register(ServiceDescriptor descriptor);
    
    OnBeforeTenantBuild<ITenantedServiceRegistrar<TServiceProviderRoot>, TServiceProviderRoot> OnBeforeBuild { get; set; }
    
    ValueTask<TServiceProviderRoot> BuildAsync();
    
    IServiceRegistrar GetOrCreateTenant(string tenantId);
}