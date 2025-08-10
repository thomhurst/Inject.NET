namespace Inject.NET.Extensions;

public static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueFactory) where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out var value))
        {
            return value;
        }
        
        value = valueFactory(key);
        dictionary[key] = value;
        return value;
    }
}