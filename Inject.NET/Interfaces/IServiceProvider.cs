namespace Inject.NET.Interfaces;

public interface IServiceProvider : IAsyncDisposable
{
    IServiceScope CreateScope();
}