﻿namespace Inject.NET.Pools;

public class ListPool<T>
{
    public static ListPool<T> Shared { get; } = new();

    private readonly Stack<List<T>> _pool = new(Enumerable.Range(0, 1024)
        .Select(_ => new List<T>()));

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
        
        list.Clear();
        _pool.Push(list);
    }
}