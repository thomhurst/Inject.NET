namespace Inject.NET.Interfaces;

public interface ITenantedServiceProvider : IServiceProvider
{
    IServiceProvider GetTenant(string tenantId);
}