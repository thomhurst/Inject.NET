namespace Inject.NET.Interfaces;

public interface IServiceProvider : IAsyncDisposable
{
    IServiceScope CreateScope();
}

public interface ITenantedServiceProvider : IServiceProvider
{
    IServiceProvider GetTenant(string tenantId);
}