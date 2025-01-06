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

    public async ValueTask Return(T? t)
    {
        if (t == null)
        {
            return;
        }

        if (await policy.ReturnAsync(t))
        {
            _pool.Push(t);
        }
    }
}