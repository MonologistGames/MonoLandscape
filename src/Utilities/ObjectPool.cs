using System;
using System.Collections.Generic;

namespace MonoLandscape.Utilities;

public class ObjectPool<T>(int size, Func<T> generator, Action<T> finalizer)
    where T : class
{
    private readonly Queue<T> _pool = new Queue<T>(size);

    public T Get()
    {
        if (_pool.Count > 0)
        {
            return _pool.Dequeue();
        }
        return generator();
    }
    
    public void Return(T obj)
    {
        finalizer(obj);
        _pool.Enqueue(obj);
    }
}