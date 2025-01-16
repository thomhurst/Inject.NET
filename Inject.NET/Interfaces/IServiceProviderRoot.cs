namespace Inject.NET.Interfaces;

public interface IServiceProviderRoot<out TScope> : IServiceProviderRoot, IServiceProvider<TScope> where TScope : IServiceScope;

public interface IServiceProviderRoot
{
    IServiceProvider GetTenant(string tenantId);
}