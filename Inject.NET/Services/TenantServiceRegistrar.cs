using System.Diagnostics.CodeAnalysis;
using Inject.NET.Delegates;
using Inject.NET.Enums;
using Inject.NET.Extensions;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public class TenantServiceRegistrar : IServiceRegistrar
{
    public ServiceFactoryBuilders ServiceFactoryBuilders { get; } = new();
    
    public IServiceRegistrar Register<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(Func<IServiceScope, Type, object> factory, Lifetime lifetime)
    {
        ServiceFactoryBuilders.Add<TService, TImplementation>(lifetime, factory);

        return this;
    }

    public IServiceRegistrar RegisterOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime)
    {
        if (implementationType.IsAssignableTo(serviceType))
        {
            throw new ArgumentException($"The implementation type {implementationType} is not assignable to {serviceType}");
        }
        
        ServiceFactoryBuilders.AddOpenGeneric(serviceType, implementationType, lifetime);

        return this;
    }

    public IServiceRegistrar RegisterKeyed<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(Func<IServiceScope, Type, object> factory, Lifetime lifetime, string key)
    {
        ServiceFactoryBuilders.Add<TService, TImplementation>(lifetime, key, factory);

        return this;
    }

    public IServiceRegistrar RegisterKeyedOpenGeneric([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type serviceType, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type implementationType, Lifetime lifetime,
        string key)
    {
        if (implementationType.IsAssignableTo(serviceType))
        {
            throw new ArgumentException($"The implementation type {implementationType} is not assignable to {serviceType}");
        }
        
        ServiceFactoryBuilders.AddOpenGeneric(serviceType, implementationType, lifetime, key);

        return this;
    }

    public OnBeforeBuild OnBeforeBuild { get; set; } = _ => { };
    
    public async ValueTask<IServiceProvider> BuildAsync(IServiceProvider rootServiceProvider)
    {
        OnBeforeBuild(this);

        var serviceProviderRoot = (ServiceProviderRoot)rootServiceProvider;
        
        var serviceProvider = new TenantServiceProvider(serviceProviderRoot, ServiceFactoryBuilders.AsReadOnly());
        
        var vt = serviceProvider.InitializeAsync();

        if (!vt.IsCompletedSuccessfully)
        {
            await vt.ConfigureAwait(false);
        }
        
        return serviceProvider;
    }
}