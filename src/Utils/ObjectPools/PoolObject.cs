using System;
using System.Collections.Generic;
using System.Text;

namespace TheColdWorld.Utils.ObjectPools;

public sealed class PoolObject<T> : IDisposable
{
    internal readonly ObjectPool<T> father;
    private bool _disposed;
    private readonly T _value;
    public T Value => _disposed ? throw new ObjectDisposedException(nameof(PoolObject<>)): _value;

    internal PoolObject(ObjectPool<T> pool,  T value)
    {
        father = pool;
        _value = value;
    }
    internal void Dispose(bool value)
    {
        _disposed = value;
        if(value ) GC.SuppressFinalize(this);
    }
    public void Dispose()
    {
        if(!_disposed)
        {
            father?.Return(this,true);
            GC.SuppressFinalize(this);
        }
    }
    ~PoolObject() => Dispose();
}
