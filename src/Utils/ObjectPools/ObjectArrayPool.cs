using System.Buffers;
using TheColdWorld.Utils.Exceptions;
using System.Linq;

namespace TheColdWorld.Utils.ObjectPools;

public sealed class ObjectArrayPool<T> : ObjectPool<T>
{
    public ObjectArrayPool(uint capacity, Func<T> constructor, ObjectCleanUpAction<T>? cleanUpAction =default)
    {
        if (capacity > 0X7FFFFFC7) throw new ArgumentOutOfRangeException($"paramator {nameof(capacity)} is larger than 0X7FFFFFC7({0X7FFFFFC7})");
        this.cleanUpAction = cleanUpAction;
        data = new T[capacity];
        rented=new bool[capacity];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = constructor.Invoke();
        }
    }
    readonly object _lock=new();
    readonly T[] data;
    readonly bool[] rented;
    readonly Dictionary<T[], int[]> rents = [];
    readonly Dictionary<PoolObject<T>, int> singleRents = [];
    readonly ObjectCleanUpAction<T>? cleanUpAction;
    public override bool TryRent(out PoolObject<T> poolObject)
    {
        lock (_lock)
        {
            for (int i = 0; i < rented.Length; i++)
            {
                if (!rented[i])
                {
                    T item = data[i]!;
                    PoolObject<T> rst = new(this, item);
                    rented[i] = true;
                    singleRents.Add(rst, i);
                    poolObject = rst;
                    return true;
                }
            }
            poolObject = null!;
            return false;
        }
    }
    public override PoolObject<T>? Rent() 
    {
        lock (_lock) {
            for (int i = 0; i < rented.Length; i++)
            {
                if (!rented[i])
                {
                    T item = data[i]!;
                    PoolObject<T> rst = new(this, item);
                    rented[i] = true;
                    singleRents.Add(rst, i);
                    return rst;
                }
            }
            return null;
        }
    }
    public T[] Rent(int minimumLength,bool throwIfNotEnough=true)
    {
        if (minimumLength == 0) return [];
        if (minimumLength < 0) throw new ArgumentOutOfRangeException(nameof(minimumLength));
        if (minimumLength > data.Length) throw new ArgumentOutOfRangeException(nameof(minimumLength));
        lock (_lock)
        {
            int[] indexes = new int[minimumLength];
            Array.Fill(indexes, -1);
            int idx = 0;
            for (int i = 0; i < rented.Length && idx < minimumLength; i++)
            {
                if (!rented[i])
                {
                    indexes[idx++] = i;
                }
            }
            if (throwIfNotEnough && indexes[^1] == -1) throw new ObjectNotEnoughException();
            T[] values = new T[indexes.Length];
            for (int i = 0; i < values.Length; i++)
            {
                int index = indexes[i];
                if (index < 0) continue;
                values[i] = data[index];
                rented[index] = true;
            }
            rents.Add(values, indexes);
            return values;
        }
    }
    public override T[] Rent(int minimumLength) => Rent(minimumLength, true);
    public override void Return(params T[] array)=> Return(array,false);
    public override void Return(T[] array, bool clearArray = false)
    {
        lock (_lock)
        {
            if (rents.TryGetValue(array,out int[] idxs))
            {
                rents.Remove(array);
                for (int i = 0; i < idxs.Length; i++)
                {
                    int idx = idxs[i];
                    cleanUpAction?.Invoke(ref data[idx]);
                    rented[idx] = false;
                }
            }
            else throw new ArgumentException("Array not from this pool.", nameof(array));
            if (clearArray) Array.Fill(array, default);
        }
    }
    public override void Return(PoolObject<T> poolObject,bool disposeing)
    {
        lock (_lock)
        {
            if(object.ReferenceEquals(poolObject.father,this)&&singleRents.TryGetValue(poolObject,out int index))
            {
                cleanUpAction?.Invoke(ref data[index]);
                rented[index] = false;
                singleRents.Remove(poolObject);
                if(!disposeing) poolObject.Dispose(true);
            }
            else throw new ArgumentException("object not from this pool.", nameof(poolObject));
        }
    }
}
