namespace Inject.NET.SourceGenerator.Extensions;

public static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
    {
        if (dictionary.TryGetValue(key, out var value))
        {
            return value;
        }

        dictionary.Add(key, defaultValue);

        return defaultValue;
    }
}