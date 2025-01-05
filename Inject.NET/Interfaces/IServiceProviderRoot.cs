namespace Inject.NET.Interfaces;

public interface IServiceProviderRoot : IServiceProvider
{
    IServiceProvider GetTenant(string tenantId);
}