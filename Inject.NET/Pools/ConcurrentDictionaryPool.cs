using System.Collections.Concurrent;

namespace Inject.NET.Pools;

public class ConcurrentDictionaryPool<TKey, TValue>(int capacity = 1000)
    where TKey : notnull
{
    public static ConcurrentDictionaryPool<TKey, TValue> Shared { get; } = new();

    private readonly Stack<ConcurrentDictionary<TKey, TValue>> _pool = new(capacity);

    public ConcurrentDictionary<TKey, TValue> Get()
    {
        if (_pool.Count > 0)
        {
            return _pool.Pop();
        }

        return [];
    }

    public void Return(ConcurrentDictionary<TKey, TValue>? dictionary)
    {
        if (dictionary == null)
        {
            return;
        }

        if (_pool.Count < capacity)
        {
            dictionary.Clear(); // Clear the dictionary before returning it to the pool
            _pool.Push(dictionary);
        }
    }
}