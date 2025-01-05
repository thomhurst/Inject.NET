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
    
    public IServiceRegistrar Register<T>(Func<IServiceScope, Type, T> factory, Lifetime lifetime)
    {
        ServiceFactoryBuilders.Add(typeof(T), lifetime, factory);

        return this;
    }

    public IServiceRegistrar RegisterKeyed<T>(Func<IServiceScope, Type, string, T> factory, Lifetime lifetime, string key)
    {
        ServiceFactoryBuilders.Add(typeof(T), lifetime, key, factory);

        return this;
    }

    public OnBeforeBuild OnBeforeBuild { get; set; } = _ => { };
    
    public async Task<IServiceProvider> BuildAsync(IServiceProvider defaultServiceProvider)
    {
        OnBeforeBuild(this);

        var serviceProvider = new TenantServiceProvider((ServiceProvider)defaultServiceProvider, ServiceFactoryBuilders.AsReadOnly());
        
        await serviceProvider.InitializeAsync();
        
        return serviceProvider;
    }
}