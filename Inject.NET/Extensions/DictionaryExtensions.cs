namespace Inject.NET.Extensions;

public static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory) where TKey : notnull
    {
        if(!dictionary.ContainsKey(key))
        {
            dictionary.TryAdd(key, valueFactory(key));
        }
        
        return dictionary[key];
    }
}