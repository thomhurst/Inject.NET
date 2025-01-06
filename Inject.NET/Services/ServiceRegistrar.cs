using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Inject.NET.Delegates;
using Inject.NET.Enums;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

public class ServiceRegistrar : ITenantedServiceRegistrar
{
    private readonly ConcurrentDictionary<string, IServiceRegistrar> _tenants = [];

    public ServiceFactoryBuilders ServiceFactoryBuilders { get; } = new();

    public ITenantedServiceRegistrar Register(ServiceDescriptor serviceDescriptor)
    {
        ServiceFactoryBuilders.Add(serviceDescriptor);

        return this;
    }

    public OnBeforeTenantBuild OnBeforeBuild { get; set; } = _ => { };

    public async ValueTask<IServiceProviderRoot> BuildAsync()
    {
        OnBeforeBuild(this);

        var serviceProvider = new ServiceProviderRoot(ServiceFactoryBuilders.AsReadOnly(), _tenants);
        
        var vt = serviceProvider.InitializeAsync();

        if (!vt.IsCompletedSuccessfully)
        {
            await vt.ConfigureAwait(false);
        }
        
        return serviceProvider;
    }

    public IServiceRegistrar GetOrCreateTenant(string tenantId)
    {
        return _tenants.GetOrAdd(tenantId, new TenantServiceRegistrar());
    }
}