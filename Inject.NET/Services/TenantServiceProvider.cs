using Inject.NET.Interfaces;
using Inject.NET.Models;

namespace Inject.NET.Services;

public abstract class TenantServiceProvider<TScope> : IServiceProvider<TScope> where TScope : IServiceScope
{
    public abstract ValueTask DisposeAsync();
    public abstract TScope CreateScope();
    
    internal abstract ServiceFactories ServiceFactories { get; }
    
    public object? GetService(Type serviceType)
    {
        return CreateScope().GetService(new ServiceKey(serviceType));
    }
}

public abstract class TenantServiceProvider<TSingletonScope, TServiceProviderRoot, TDefaultSingletonScope, TDefaultScope> : TenantServiceProvider<TDefaultScope>
    where TServiceProviderRoot : ServiceProviderRoot<TServiceProviderRoot, TDefaultSingletonScope, TDefaultScope>
    where TSingletonScope : TenantedSingletonScope<TSingletonScope, TServiceProviderRoot, TDefaultSingletonScope, TDefaultScope>, IServiceScope
    where TDefaultScope : ServiceScope<TServiceProviderRoot, TDefaultSingletonScope, TDefaultScope>
    where TDefaultSingletonScope : SingletonScope<TDefaultSingletonScope, TServiceProviderRoot, TDefaultScope>
{
    public TServiceProviderRoot Root { get; }
    internal override ServiceFactories ServiceFactories { get; }
    
    public TenantServiceProvider(TServiceProviderRoot rootServiceProviderRoot, ServiceFactories serviceFactories)
    {
        Root = rootServiceProviderRoot;
        ServiceFactories = serviceFactories;
    }
    
    public abstract TSingletonScope SingletonScope { get; }

    public virtual async ValueTask InitializeAsync()
    {
        SingletonScope.PreBuild();

        await using var scope = CreateScope();
        
        foreach (var type in ServiceFactories.Descriptors.Keys)
        {
            scope.GetService(type);
        }
        
        await SingletonScope.FinalizeAsync();
    }
    
    public override async ValueTask DisposeAsync()
    {
        await SingletonScope.DisposeAsync();
    }
}