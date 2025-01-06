namespace Inject.NET.Pools;

public class DictionaryPool<TKey, TValue> where TKey : notnull
{
    public static DictionaryPool<TKey, TValue> Shared { get; } = new();

    private readonly Stack<Dictionary<TKey, TValue>> _pool = new(1024);

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

        dictionary.Clear();
        _pool.Push(dictionary);
    }
}