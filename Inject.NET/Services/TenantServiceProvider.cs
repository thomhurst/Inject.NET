using System.Reflection;
using Inject.NET.Interfaces;
using Inject.NET.Models;
using IServiceProvider = Inject.NET.Interfaces.IServiceProvider;

namespace Inject.NET.Services;

public abstract class TenantServiceProvider : IServiceProvider
{
    internal abstract ServiceFactories ServiceFactories { get; }
    
    public abstract ValueTask DisposeAsync();
    public abstract IServiceScope CreateScope();
}

public class TenantServiceProvider<TServiceProvider, TSingletonScope> : TenantServiceProvider
    where TServiceProvider : ServiceProviderRoot<TServiceProvider, TSingletonScope>
    where TSingletonScope : SingletonScope, IServiceScope
{
    public readonly TSingletonScope SingletonScope;
    public readonly TServiceProvider RootServiceProviderRoot;

    public TenantServiceProvider(TServiceProvider rootServiceProviderRoot, ServiceFactories serviceFactories)
    {
        RootServiceProviderRoot = rootServiceProviderRoot;
        ServiceFactories = serviceFactories;
        // TODO - Improve?
        SingletonScope = (TSingletonScope)typeof(TSingletonScope).GetConstructors(BindingFlags.Default | BindingFlags.Instance).First().Invoke([this, rootServiceProviderRoot, serviceFactories]);
    }

    internal async ValueTask InitializeAsync()
    {
        SingletonScope.PreBuild();

        await using var scope = CreateScope();
        
        foreach (var type in ServiceFactories.Descriptors.Keys)
        {
            scope.GetService(type);
        }
        
        await SingletonScope.FinalizeAsync();
    }

    internal override ServiceFactories ServiceFactories { get; }

    public override async ValueTask DisposeAsync()
    {
        await SingletonScope.DisposeAsync();
    }

    public override IServiceScope CreateScope()
    {
        return new TenantedScope<TServiceProvider, TSingletonScope>(this, (ServiceScope<TServiceProvider, TSingletonScope>)RootServiceProviderRoot.CreateScope(), SingletonScope, ServiceFactories);
    }
}