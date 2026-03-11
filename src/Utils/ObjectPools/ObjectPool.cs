using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace TheColdWorld.Utils.ObjectPools;

public abstract class ObjectPool<T> : ArrayPool<T>
{
    public abstract bool TryRent(out PoolObject<T> poolObject);
    public abstract PoolObject<T>? Rent();
    public virtual void Return(params T[] array) => Return(array, false);
    public abstract void Return(PoolObject<T> poolObject,bool disposing=false);
}
