using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface IServiceScope : IAsyncDisposable, IDisposable, System.IServiceProvider
{
    object? GetService(ServiceKey serviceKey);
    IReadOnlyList<object> GetServices(ServiceKey serviceKey);
    
    IServiceScope SingletonScope { get; }
    IServiceProvider ServiceProvider { get; }
}