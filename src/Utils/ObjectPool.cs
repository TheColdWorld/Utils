using System.Buffers;


namespace TheColdWorld.Utils
{
    public delegate void ObjectPoolRemoveAction<R>(ref R instance);
    public sealed class ObjectPool<T>(Func<T> constructor, ObjectPoolRemoveAction<T>? removeEvent = null) : ArrayPool<T>
    {
        private readonly ObjectPoolRemoveAction<T>? removeEvent = removeEvent;
        private readonly Func<T> constructor = constructor;
        private readonly object _lock = new();
        private readonly Dictionary<T, bool> data = [];
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
                if (index < minimumLength - 1)
                {
                    for (; index < minimumLength; index++)
                    {
                        T asyncEventArgs = constructor.Invoke();
                        data[asyncEventArgs] = true;
                        result[index] = asyncEventArgs;
                    }
                }
                return result;
            }
        }
        public override void Return(T[] array, bool clearArray = false)
        {
            ObjectUtil.ThrowIfNull(array, nameof(array));
            lock (_lock)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    T item = array[i];
                    if (item is null || !data.ContainsKey(item)) continue;
                    removeEvent?.Invoke(ref item);
                    data[item] = false;
                }
                if (clearArray) Array.Fill(array, default);
            }
        }
    }
}
