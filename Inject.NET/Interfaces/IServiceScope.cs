namespace Inject.NET.Interfaces;

public interface IServiceScope : IAsyncDisposable, System.IServiceProvider
{
    IEnumerable<object> GetServices(Type type);
    
    object? GetService(Type type, string? key);
    IEnumerable<object> GetServices(Type type, string? key);
    
    IServiceProvider ServiceProvider { get; }
}