using Inject.NET.Models;
using Microsoft.Extensions.ObjectPool;

namespace Inject.NET;

internal class Pools
{
    public static readonly ObjectPool<List<object>> DisposalTracker =
        ObjectPool.Create(new ListPoolPolicy<object>());
    
    public static readonly ObjectPool<Dictionary<ServiceKey, object>> Objects =
        ObjectPool.Create(new DictionaryPoolPolicy<Dictionary<ServiceKey, object>, ServiceKey, object>());
    
    public static readonly ObjectPool<Dictionary<ServiceKey, List<object>>> Enumerables =
        ObjectPool.Create(new DictionaryPoolPolicy<Dictionary<ServiceKey, List<object>>, ServiceKey, List<object>>());
    
    public static readonly ObjectPool<Dictionary<ServiceKey, Func<object>>> Funcs =
        ObjectPool.Create(new DictionaryPoolPolicy<Dictionary<ServiceKey, Func<object>>, ServiceKey, Func<object>>());
    
    public static readonly ObjectPool<Dictionary<ServiceKey, List<Func<object>>>> EnumerableFuncs =
        ObjectPool.Create(new DictionaryPoolPolicy<Dictionary<ServiceKey, List<Func<object>>>, ServiceKey, List<Func<object>>>());
}

public class ListPoolPolicy<T> : IPooledObjectPolicy<List<T>>
{
    public List<T> Create()
    {
        return [];
    }

    public bool Return(List<T> obj)
    {
        obj.Clear();
        return true;
    }
}

public class DictionaryPoolPolicy<TDictionary, TKey, TValue> : IPooledObjectPolicy<TDictionary>
    where TDictionary : IDictionary<TKey, TValue>, new()
{
    public TDictionary Create()
    {
        return new TDictionary();
    }

    public bool Return(TDictionary obj)
    {
        obj.Clear();
        return true;
    }
}