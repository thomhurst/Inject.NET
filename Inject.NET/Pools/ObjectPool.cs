namespace Inject.NET.Pools;

internal class ObjectPool<T>(IPooledObjectPolicy<T> policy)
{
    private readonly Stack<T> _pool = new(1024);

    public T Get()
    {
        if (_pool.TryPop(out var item))
        {
            return item;
        }

        return policy.Create();
    }

    public void Return(T? t)
    {
        if (t == null)
        {
            return;
        }

        var vt = policy.ReturnAsync(t);
        
        if (!vt.IsCompletedSuccessfully)
        {
            _ = Await(vt, t);
            return;
        }
        
        if(vt.Result)
        {
            _pool.Push(t);
        }
    }

    private async Task Await(ValueTask<bool> valueTask, T t)
    {
        if (await valueTask)
        {
            _pool.Push(t);
        }
    }
}