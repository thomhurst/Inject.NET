namespace Inject.NET.Pools;

public class DictionaryPool<TKey, TValue>(int capacity = 1000)
    where TKey : notnull
{
    public static DictionaryPool<TKey, TValue> Shared { get; } = new();

    private readonly Stack<Dictionary<TKey, TValue>> _pool = new(capacity);

    public Dictionary<TKey, TValue> Get()
    {
        if (_pool.Count > 0)
        {
            return _pool.Pop();
        }

        return [];
    }

    public void Return(Dictionary<TKey, TValue>? dictionary)
    {
        if (dictionary == null)
        {
            return;
        }

        if (_pool.Count < capacity)
        {
            dictionary.Clear();
            _pool.Push(dictionary);
        }
    }
}