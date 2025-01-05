namespace Inject.NET.Pools;

public class ListPool<T>(int capacity = 1000)
{
    public static ListPool<T> Shared { get; } = new();

    private readonly Stack<List<T>> _pool = new(capacity);

    public List<T> Get()
    {
        if (_pool.Count > 0)
        {
            return _pool.Pop();
        }

        return [];
    }

    public void Return(List<T>? list)
    {
        if (list == null)
        {
            return;
        }
        
        if (_pool.Count < capacity)
        {
            list.Clear();
            _pool.Push(list);
        }
    }
}