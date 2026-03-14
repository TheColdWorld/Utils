using System.Buffers;
using System.Runtime.CompilerServices;


namespace TheColdWorld.Utils.ObjectPools;

public delegate void ObjectCleanUpAction<R>(ref R instance);
public sealed class ObjectListPool<T>(Func<T> constructor, ObjectCleanUpAction<T>? cleanEvent = null) : ObjectPool<T> where T : class
{
    private volatile bool _disposed=true;
    private readonly ObjectCleanUpAction<T>? removeEvent = cleanEvent;
    private readonly Func<T> constructor = constructor;
    private readonly object _lock = new();
    private readonly Dictionary<T, bool> data = [];
    private readonly HashSet<PoolObject<T>> poolObjects = [];
    public override bool TryRent(out PoolObject<T> poolObject)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(data));
        lock (_lock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(data));
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
                poolObjects.Add(poolObject);
                return true;
            }
            result = constructor.Invoke();
            data.Add(result, true);
            poolObject= new(this, result);
            poolObjects.Add(poolObject);
            return true;
        }
    }
    public override PoolObject<T> Rent()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(data));
        lock (_lock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(data));
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
            PoolObject<T> poolObject = new(this, result);
            poolObjects.Add(poolObject);
            return poolObject;
        }
    }
    public override T[] Rent(int minimumLength)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(data));
        lock (_lock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(data));
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
        if (_disposed) throw new ObjectDisposedException(nameof(data));
        ObjectUtil.ThrowIfNull(array, nameof(array));
        lock (_lock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(data));
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

    public override void Return(PoolObject<T> poolObject,bool disposing=false)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(data));
        lock (_lock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(data));
            T v = poolObject.Value;
            if (object.ReferenceEquals(poolObject.father, this) && data.ContainsKey(v))
            {
                removeEvent?.Invoke(ref v);
                data[v] = false;
                poolObjects.Remove(poolObject);
                if (!disposing) poolObject.Dispose(true);
            }
            else throw new ArgumentException("object not from this pool.", nameof(poolObject));
        }
    }
    public override void Dispose() {

        this._disposed = true;
        lock (_lock)
        {
            foreach (T item in this.data.Keys)
            {
                if(item is IDisposable disposable) disposable.Dispose();
                if (item is IAsyncDisposable asyncDisposable) asyncDisposable.DisposeAsync().GetAwaiter().GetResult();
            }
            data.Clear();
            foreach (PoolObject<T> item in poolObjects)
            {
                item.Dispose(true);
            }
            poolObjects.Clear();
        }
    }
}
