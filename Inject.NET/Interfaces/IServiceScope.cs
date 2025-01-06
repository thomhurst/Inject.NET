using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface IServiceScope : IAsyncDisposable, System.IServiceProvider
{
    object? GetService(ServiceKey serviceKey);
    IEnumerable<object> GetServices(ServiceKey serviceKey);
    
    IServiceProvider ServiceProvider { get; }
}