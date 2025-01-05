using Inject.NET.Delegates;
using Inject.NET.Enums;
using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface IServiceRegistrar
{
    ServiceFactoryBuilders ServiceFactoryBuilders { get; }
    
    IServiceRegistrar Register<T>(Func<IServiceScope, Type, T> factory, Lifetime lifetime);
    
    IServiceRegistrar RegisterOpenGeneric(Type serviceType, Type implementationType, Lifetime lifetime);
    
    IServiceRegistrar RegisterKeyed<T>(Func<IServiceScope, Type, string, T> factory, Lifetime lifetime, string key);
    
    IServiceRegistrar RegisterKeyedOpenGeneric(Type serviceType, Type implementationType, Lifetime lifetime, string key);

    
    OnBeforeBuild OnBeforeBuild { get; set; }
    
    Task<IServiceProvider> BuildAsync(IServiceProvider rootServiceProvider);
}