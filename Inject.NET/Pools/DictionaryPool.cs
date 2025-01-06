namespace Inject.NET.Pools;

public class DictionaryPool<TKey, TValue> where TKey : notnull
{
    public static DictionaryPool<TKey, TValue> Shared { get; } = new();

    private readonly Stack<Dictionary<TKey, TValue>> _pool =
        new(Enumerable.Range(0, 64).Select(_ => new Dictionary<TKey, TValue>()));

    public Dictionary<TKey, TValue> Get()
    {
        if (_pool.TryPop(out var item))
        {
            return item;
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