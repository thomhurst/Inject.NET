using Inject.NET.Models;

namespace Inject.NET.Interfaces;

public interface IServiceScope : IAsyncDisposable, System.IServiceProvider
{
    object? GetService(ServiceKey serviceKey);
    
    IServiceProvider ServiceProvider { get; }
}