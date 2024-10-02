using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CarinaStudio.AppSuite.ObjectPool;

/// <summary>
/// Specialized <see cref="ObjectPool"/> for <see cref="List{T}"/>.
/// </summary>
/// <param name="capacity">Capacity of pool.</param>
public class ListPool<T>(int capacity) : DefaultObjectPool<List<T>>(new ObjectPolicy(), capacity)
{
    // Implementation of IPooledObjectPolicy.
    class ObjectPolicy : IPooledObjectPolicy<List<T>>
    {
        public List<T> Create() => new();
        public bool Return(List<T> list)
        {
            list.Clear();
            return true;
        } 
    }

    
    /// <summary>
    /// Get a list from the pool.
    /// </summary>
    /// <param name="elements">Initial elements.</param>
    /// <returns><see cref="List{t}"/>.</returns>
    public List<T> Get(IEnumerable<T> elements)
    {
        var list = this.Get();
        list.AddRange(elements);
        return list;
    }
    
    
    /// <summary>
    /// Get a list from the pool then use and return it.
    /// </summary>
    /// <param name="elements">Initial elements.</param>
    /// <param name="action">Action to use the list.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetAndUse(IEnumerable<T> elements, Action<List<T>> action)
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
    /// Get a list from the pool then use and return it.
    /// </summary>
    /// <param name="elements">Initial elements.</param>
    /// <param name="action">Function to use the list and generate value.</param>
    /// <typeparam name="R">Type of generated value.</typeparam>
    /// <returns>Generated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public R GetAndUse<R>(IEnumerable<T> elements, Func<List<T>, R> action)
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