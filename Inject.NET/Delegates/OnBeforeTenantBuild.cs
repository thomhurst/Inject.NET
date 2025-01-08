using Inject.NET.Interfaces;

namespace Inject.NET.Delegates;

public delegate void OnBeforeTenantBuild<in TTenantedServiceRegistrar, TServiceProviderRoot>(TTenantedServiceRegistrar serviceRegistrar) 
    where TTenantedServiceRegistrar : ITenantedServiceRegistrar<TServiceProviderRoot> 
    where TServiceProviderRoot : IServiceProviderRoot;