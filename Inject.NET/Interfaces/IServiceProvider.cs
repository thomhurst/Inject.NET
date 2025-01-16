namespace Inject.NET.Interfaces;

public interface IServiceProvider<out TScope> : IAsyncDisposable, IServiceProvider
where TScope : IServiceScope
{
    TScope CreateTypedScope();
}

public interface IServiceProvider : System.IServiceProvider
{
    IServiceScope CreateScope();
}