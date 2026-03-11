using System.Buffers;
using System.Runtime.CompilerServices;


namespace TheColdWorld.Utils.ObjectPools;

public delegate void ObjectCleanUpAction<R>(ref R instance);
public sealed class ObjectListPool<T>(Func<T> constructor, ObjectCleanUpAction<T>? cleanEvent = null) : ObjectPool<T> where T : class
{
    private readonly ObjectCleanUpAction<T>? removeEvent = cleanEvent;
    private readonly Func<T> constructor = constructor;
    private readonly object _lock = new();
    private readonly Dictionary<T, bool> data = [];
    public override bool TryRent(out PoolObject<T> poolObject)
    {
        lock (_lock)
        {
            T? result = default;
            foreach (var item in data)
            {
                if (!item.Value)
                {
                    result = item.Key;
                    break;
                }
            }
            if (result is not null)
            {
                data[result] = true;
                poolObject= new(this, result);
                return true;
            }
            result = constructor.Invoke();
            data.Add(result, true);
            poolObject= new(this, result);
            return true;
        }
    }
    public override PoolObject<T> Rent()
    {
        lock (_lock)
        {
            T? result = default;
            foreach (var item in data)
            {
                if(!item.Value)
                {
                    result = item.Key;
                    break;
                }
            }
            if(result is not null)
            {
                data[result] = true;
                return new (this,result);
            }
            result=constructor.Invoke();
            data.Add(result, true);
            return new(this,result);
        }
    }
    public override T[] Rent(int minimumLength)
    {
        lock (_lock)
        {
            int index = 0;
            T[] result = new T[minimumLength];
            foreach (var item in data)
            {
                if (!item.Value)
                {
                    result[index++] = item.Key;
                }
                if (index == minimumLength) break;
            }
            if (index < minimumLength)
            {
                for (; index < minimumLength; index++)
                {
                    T item = constructor.Invoke();
                    data[item] = true;
                    result[index] = item;
                }
            }
            return result;
        }
    }
    public override void Return(params T[] array) => Return(array, false);
    public override void Return(T[] array, bool clearArray = false)
    {
        ObjectUtil.ThrowIfNull(array, nameof(array));
        lock (_lock)
        {
            for (int i = 0; i < array.Length; i++)
            {
                T item = array[i];
                if (item is null || !data.ContainsKey(item)) continue;
                T original = item;
                removeEvent?.Invoke(ref item);
                data[original] = false;
            }
            if (clearArray) Array.Fill(array, default);
        }
    }

    public override void Return(PoolObject<T> poolObject,bool disposing)
    {
        T v = poolObject.Value;
        if (object.ReferenceEquals(poolObject.father,this )&& data.ContainsKey(v)) {
            removeEvent?.Invoke(ref v);
            data[v] = false;
            if (!disposing) poolObject.Dispose(true);
        }
        else throw new ArgumentException("object not from this pool.", nameof(poolObject));
    }
}
