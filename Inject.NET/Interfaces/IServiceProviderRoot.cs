namespace Inject.NET.Interfaces;

public interface IServiceProviderRoot<out TScope> : IServiceProvider<TScope> where TScope : IServiceScope
{
    IServiceProvider GetTenant(string tenantId);
}