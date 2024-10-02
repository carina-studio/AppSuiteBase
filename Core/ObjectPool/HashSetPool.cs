using CarinaStudio.Collections;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CarinaStudio.AppSuite.ObjectPool;

/// <summary>
/// Specialized <see cref="ObjectPool"/> for <see cref="HashSet{T}"/>.
/// </summary>
/// <param name="capacity">Capacity of pool.</param>
public class HashSetPool<T>(int capacity) : DefaultObjectPool<HashSet<T>>(new ObjectPolicy(), capacity)
{
    // Implementation of IPooledObjectPolicy.
    class ObjectPolicy : IPooledObjectPolicy<HashSet<T>>
    {
        public HashSet<T> Create() => new();
        public bool Return(HashSet<T> list)
        {
            list.Clear();
            return true;
        } 
    }

    
    /// <summary>
    /// Get a set from the pool.
    /// </summary>
    /// <param name="elements">Initial elements.</param>
    /// <returns><see cref="HashSet{t}"/>.</returns>
    public HashSet<T> Get(IEnumerable<T> elements)
    {
        var list = this.Get();
        list.AddAll(elements);
        return list;
    }
    
    
    /// <summary>
    /// Get a set from the pool then use and return it.
    /// </summary>
    /// <param name="elements">Initial elements.</param>
    /// <param name="action">Action to use the set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetAndUse(IEnumerable<T> elements, Action<HashSet<T>> action)
    {
        var collection = this.Get(elements);
        try
        {
            action(collection);
        }
        finally
        {
            this.Return(collection);
        }
    }
    
    
    /// <summary>
    /// Get a set from the pool then use and return it.
    /// </summary>
    /// <param name="elements">Initial elements.</param>
    /// <param name="action">Function to use the set and generate value.</param>
    /// <typeparam name="R">Type of generated value.</typeparam>
    /// <returns>Generated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public R GetAndUse<R>(IEnumerable<T> elements, Func<HashSet<T>, R> action)
    {
        var collection = this.Get(elements);
        try
        {
            return action(collection);
        }
        finally
        {
            this.Return(collection);
        }
    }
}