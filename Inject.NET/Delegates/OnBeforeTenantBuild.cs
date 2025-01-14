using Inject.NET.Interfaces;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Delegates;

public delegate void OnBeforeTenantBuild<in TTenantedServiceRegistrar, TServiceProvider>(TTenantedServiceRegistrar serviceRegistrar) 
    where TTenantedServiceRegistrar : ITenantedServiceRegistrar<TServiceProvider> 
    where TServiceProvider : IServiceProvider;