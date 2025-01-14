using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public abstract class TenantServiceProvider : IServiceProvider
{
    public abstract ValueTask DisposeAsync();
    public abstract IServiceScope CreateScope();
    
    internal abstract ServiceFactories ServiceFactories { get; }
}

public abstract class TenantServiceProvider<TSingletonScope, TServiceProviderRoot, TDefaultSingletonScope, TDefaultScope> : TenantServiceProvider
    where TServiceProviderRoot : ServiceProviderRoot<TServiceProviderRoot, TDefaultSingletonScope>
    where TSingletonScope : TenantedSingletonScope<TSingletonScope, TServiceProviderRoot, TDefaultSingletonScope, TDefaultScope>, IServiceScope
    where TDefaultScope : ServiceScope<TServiceProviderRoot, TDefaultSingletonScope>
    where TDefaultSingletonScope : SingletonScope
{
    public TServiceProviderRoot Root { get; }
    internal override ServiceFactories ServiceFactories { get; }
    
    public TenantServiceProvider(TServiceProviderRoot rootServiceProviderRoot, ServiceFactories serviceFactories)
    {
        Root = rootServiceProviderRoot;
        ServiceFactories = serviceFactories;
    }
    
    public abstract TSingletonScope SingletonScope { get; }

    public async ValueTask InitializeAsync()
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

    public override IServiceScope CreateScope()
    {
        return new TenantedScope<TServiceProviderRoot, TSingletonScope, TDefaultSingletonScope, TDefaultScope>(Root, (TDefaultScope)Root.CreateScope(), Root.SingletonScope, SingletonScope, ServiceFactories);
    }
}