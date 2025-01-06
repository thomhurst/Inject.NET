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

    public ValueTask Return(T? t)
    {
        if (t == null)
        {
            return default;
        }

        var vt = policy.ReturnAsync(t);
        
        if (!vt.IsCompletedSuccessfully)
        {
            return Await(vt, t);
        }
        
        if(vt.Result)
        {
            _pool.Push(t);
        }

        return default;
    }

    private async ValueTask Await(ValueTask<bool> valueTask, T t)
    {
        if (await valueTask)
        {
            _pool.Push(t);
        }
    }
}