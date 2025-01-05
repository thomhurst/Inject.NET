namespace Inject.NET.Helpers;

internal static class Disposer
{
    public static ValueTask DisposeAsync(object? obj)
    {
        if (obj is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        if (obj is IDisposable disposable)
        {
            disposable.Dispose();
        }
        
        return ValueTask.CompletedTask;
    }
}