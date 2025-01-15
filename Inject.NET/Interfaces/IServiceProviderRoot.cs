namespace Inject.NET.Interfaces;

public interface IServiceProviderRoot<out TScope> : IServiceProvider<TScope> where TScope : IServiceScope
{
    IServiceProvider<TTenantScope> GetTenant<TTenantScope>(string tenantId) where TTenantScope : IServiceScope;
}