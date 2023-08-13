using System;
using System.Collections.Generic;

namespace ZoneTitles.Common;

public class Pool<T>
{
    private Func<T> _factory;
    private Queue<T> _available = new Queue<T>();
    private HashSet<T> _allocated = new HashSet<T>();

    public Pool(Func<T> factory)
    {
        _factory = factory;
    }

    public T Allocate()
    {
        T item;
        if (!_available.TryDequeue(out item))
        {
            item = _factory();
        }
        
        _allocated.Add(item);
        return item;
    }
    
    public void Free(T item)
    {
        if (_allocated.Remove(item))
        {
            _available.Enqueue(item);
        }
    }
}