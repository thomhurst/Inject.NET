namespace Inject.NET.Interfaces;

public interface IServiceProvider<out TScope> : IAsyncDisposable, IServiceProvider
where TScope : IServiceScope
{
    TScope CreateScope();
}