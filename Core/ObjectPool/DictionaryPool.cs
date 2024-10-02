using Microsoft.Extensions.ObjectPool;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CarinaStudio.AppSuite.ObjectPool;

/// <summary>
/// Specialized <see cref="ObjectPool"/> for <see cref="Dictionary{TKey, TValue}"/>.
/// </summary>
/// <param name="capacity">Capacity of pool.</param>
public class DictionaryPool<TKey, TValue>(int capacity) : DefaultObjectPool<Dictionary<TKey, TValue>>(new ObjectPolicy(), capacity) where TKey : notnull
{
    // Implementation of IPooledObjectPolicy.
    class ObjectPolicy : IPooledObjectPolicy<Dictionary<TKey, TValue>>
    {
        public Dictionary<TKey, TValue> Create() => new();
        public bool Return(Dictionary<TKey, TValue> dictionary)
        {
            dictionary.Clear();
            return true;
        } 
    }

    
    /// <summary>
    /// Get a dictionary from the pool.
    /// </summary>
    /// <param name="keyValues">Initial key-value pairs.</param>
    /// <returns><see cref="Dictionary{TKey, TValue}"/>.</returns>
    public Dictionary<TKey, TValue> Get(IDictionary<TKey, TValue> keyValues)
    {
        var dictionary = this.Get();
        foreach (var (key, value) in keyValues)
            dictionary[key] = value;
        return dictionary;
    }
    
    
    /// <summary>
    /// Get a dictionary from the pool then use and return it.
    /// </summary>
    /// <param name="keyValues">Initial key-value pairs.</param>
    /// <param name="action">Action to use the dictionary.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetAndUse(IDictionary<TKey, TValue> keyValues, Action<Dictionary<TKey, TValue>> action)
    {
        var collection = this.Get(keyValues);
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
    /// Get a dictionary from the pool then use and return it.
    /// </summary>
    /// <param name="keyValues">Initial key-value pairs.</param>
    /// <param name="action">Function to use the dictionary and generate value.</param>
    /// <typeparam name="R">Type of generated value.</typeparam>
    /// <returns>Generated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public R GetAndUse<R>(IDictionary<TKey, TValue> keyValues, Func<Dictionary<TKey, TValue>, R> action)
    {
        var collection = this.Get(keyValues);
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