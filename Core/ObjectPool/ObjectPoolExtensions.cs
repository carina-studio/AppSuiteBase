using System;
using System.Runtime.CompilerServices;

namespace CarinaStudio.AppSuite.ObjectPool;

/// <summary>
/// Extension methods for <see cref="Microsoft.Extensions.ObjectPool.ObjectPool{T}"/>.
/// </summary>
public static class ObjectPoolExtensions
{
    /// <summary>
    /// Get an object from the pool then use and return it.
    /// </summary>
    /// <param name="pool">Object pool.</param>
    /// <param name="action">Action to use the object.</param>
    /// <typeparam name="T">Type of the object.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetAndUse<T>(this Microsoft.Extensions.ObjectPool.ObjectPool<T> pool, Action<T> action) where T : class
    {
        var collection = pool.Get();
        try
        {
            action(collection);
        }
        finally
        {
            pool.Return(collection);
        }
    }
    
    
    /// <summary>
    /// Get an object from the pool then use and return it.
    /// </summary>
    /// <param name="pool">Object pool.</param>
    /// <param name="action">Function to use the object and generate value.</param>
    /// <typeparam name="T">Type of the object.</typeparam>
    /// <typeparam name="R">Type of generated value.</typeparam>
    /// <returns>Generated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static R GetAndUse<T, R>(this Microsoft.Extensions.ObjectPool.ObjectPool<T> pool, Func<T, R> action) where T : class
    {
        var collection = pool.Get();
        try
        {
            return action(collection);
        }
        finally
        {
            pool.Return(collection);
        }
    }
}