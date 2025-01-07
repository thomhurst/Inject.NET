using Inject.NET.Delegates;
using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface IServiceRegistrar
{
    ServiceFactoryBuilders ServiceFactoryBuilders { get; }
    
    IServiceRegistrar Register(ServiceDescriptor descriptor);
    
    OnBeforeBuild OnBeforeBuild { get; set; }
    
    ValueTask<IServiceProvider> BuildAsync(IServiceProvider rootServiceProvider);
}