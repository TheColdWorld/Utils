namespace TheColdWorld.Utils;
/// <summary>
/// Utils for <see cref="IEnumerable{T}"/>
/// </summary>
public static class EnumerationUtil
{
    /// <summary>
    /// get the first item from <paramref name="source"/>
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> thrown if the <paramref name="source"/> is empty</typeparam>
    /// <param name="exceptionConstructor">Construction of <typeparamref name="TException"/> if <paramref name="source"/> is empty</param>
    /// <returns>First item of <paramref name="source"/></returns>
    public static TResult FirstOrThrow<TResult, TException>(this IEnumerable<TResult> source, Func<TException> exceptionConstructor) where TException : Exception
    {
        var result = source.TryGetFirst(out bool present);
        return !present ? throw exceptionConstructor() : result!;
    }
    /// <summary>
    /// get the first item from <paramref name="source"/>
    /// </summary>
    /// <param name="found"><see langword="true"/> if <paramref name="source"/> is not empty</param>
    /// <returns>First item of <paramref name="source"/> if <paramref name="source"/> is not empty,otherwise <see langword="null"/></returns>
    /// <exception cref="ArgumentNullException">if <paramref name="source"/> is <see langword="null"/></exception>
    public static TResult? TryGetFirst<TResult>(this IEnumerable<TResult> source, out bool found)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (source is IList<TResult> list)
        {
            if (list.Count > 0)
            {
                found = true;
                return list[0];
            }
        }
        else
        {
            using var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                found = true;
                return enumerator.Current;

            }
        }
        found = false;
        return default;
    }
    /// <summary>
    /// get the lase item from <paramref name="source"/>
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> thrown if the <paramref name="source"/> is empty</typeparam>
    /// <param name="exceptionConstructor">Construction of <typeparamref name="TException"/> if <paramref name="source"/> is empty</param>
    /// <returns>First item of <paramref name="source"/></returns>
    public static TResult LastOrThrow<TResult, TException>(this IEnumerable<TResult> source, Func<TException> exceptionConstucter) where TException : Exception
    {
        var r = source.TryGetLast(out bool present);
        return !present ? throw exceptionConstucter() : r!;
    }
    /// <summary>
    /// get the last item from <paramref name="source"/>
    /// </summary>
    /// <param name="found"><see langword="true"/> if <paramref name="source"/> is not empty</param>
    /// <returns>First item of <paramref name="source"/> if <paramref name="source"/> is not empty,otherwise <see langword="null"/></returns>
    /// <exception cref="ArgumentNullException">if <paramref name="source"/> is <see langword="null"/></exception>
    public static TResult? TryGetLast<TResult>(this IEnumerable<TResult> source, out bool found)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (source is IList<TResult> list)
        {
            if (list.Count > 0)
            {
                found = true;
                return list[^1];
            }
        }
        else
        {
            var enumerator = source.GetEnumerator();
            if (enumerator.MoveNext())
            {
                TResult result = enumerator.Current;
                while (enumerator.MoveNext()) result = enumerator.Current;
                found = true;
                return result;
            }
        }
        found = false;
        return default;
    }
    /// <summary>
    /// get the first item from <paramref name="source"/> or the <see langword="value"/> of <paramref name="orElseGetter"/>
    /// </summary>
    /// <param name="orElseGetter">The getter if <paramref name="source"/> is empty</param>
    /// <returns>First item of <paramref name="source"/> if exists ,otherwise <paramref name="orElseGetter"/>.Invoke()</returns>
    public static TResult FirstOrDefault<TResult>(this IEnumerable<TResult> source, Func<TResult> orElseGetter)
    {
        var rst = source.TryGetFirst(out bool present);
        return !present ? orElseGetter() : rst!;
    }
    /// <summary>
    /// get the last item from <paramref name="source"/> or the <see langword="value"/> of <paramref name="orElseGetter"/>
    /// </summary>
    /// <param name="orElseGetter">The getter if <paramref name="source"/> is empty</param>
    /// <returns>Last item of <paramref name="source"/> if exists ,otherwise <paramref name="orElseGetter"/>.Invoke()</returns>
    public static TResult LastOrDefault<TResult>(this IEnumerable<TResult> source, Func<TResult> orElseGetter)
    {
        var rst = source.TryGetLast(out bool present);
        return !present ? orElseGetter() : rst!;
    }
    /// <summary>
    /// get the index in <paramref name="item"/> that size in <see cref="long"/>
    /// </summary>
    /// <param name="src">source of item</param>
    /// <param name="item">the item that will find index in <paramref name="src"/></param>
    /// <returns>a <see cref="long"/>(value >= 0) about <paramref name="item"/>'s index of <paramref name="src"/>,otherwise <see langword="-1"/></returns>
    /// <seealso cref="LongIndexesOf"/>
    public static long LongIndexOf<T>(this IEnumerable<T> src, ref T item)
    {
        IEnumerator<T> enumerator = src.GetEnumerator();
        enumerator.MoveNext();
        long index = 0;bool found = false;
        do
        {
            if (object.ReferenceEquals(item, enumerator.Current)) break;
            index++;
        }
        while (enumerator.MoveNext());
        return found ? index : -1;
    }
    public static long[] LongIndexesOf<T>(this IEnumerable<T> src, T[] items) {
        long[] result = new long[items.LongLength];
        Array.Fill(result, -1L);
        for (long i = 0; i < items.LongLength; i++)
        {
            T item = items[i];
            if(LongContains(src, item,out long idx)) result[i]=idx;
        }
        return result;
    }
    /// <summary>
    /// get the index in <paramref name="item"/> that size in <see cref="int"/>
    /// </summary>
    /// <param name="src">source of item</param>
    /// <param name="item">the item that will find index in <paramref name="src"/></param>
    /// <returns>a <see cref="int"/>(value >= 0) about <paramref name="item"/>'s index of <paramref name="src"/>,otherwise <see langword="-1"/></returns>
    /// <seealso cref="IndexesOf"/>
    public static int IndexOf<T>(this IEnumerable<T> src, ref T item)
    {
        IEnumerator<T> enumerator = src.GetEnumerator();
        enumerator.MoveNext();
        int index = 0; bool found = false;
        do
        {
            if (object.ReferenceEquals(item, enumerator.Current)) break;
            index++;
        }
        while (enumerator.MoveNext());
        return found ? index : -1;
    }
    public static int[] IndexesOf<T>(this IEnumerable<T> src, T[] items)
    {
        int[] result = new int[items.Length];
        Array.Fill(result, -1);
        for (long i = 0; i < items.Length; i++)
        {
            T item = items[i];
            if (Contains(src, item, out int idx)) result[i] = idx;
        }
        return result;
    }
    public static bool Contains<T>(this IEnumerable<T> source, T item,out int index)
    {
        ObjectUtil.ThrowIfNull(source, nameof(source));
        ObjectUtil.ThrowIfNull(item, nameof(item));
        IEnumerator<T> enumerator = source.GetEnumerator();
        enumerator.MoveNext();
        index = 0;
        do
        {
            if (object.ReferenceEquals(enumerator.Current, item)) return true;
            index++;
        }
        while (enumerator.MoveNext());
        index = -1;
        return false;
    }
    public static bool LongContains<T>(this IEnumerable<T> source, T item,out long index)
    {
        ObjectUtil.ThrowIfNull(source, nameof(source));
        ObjectUtil.ThrowIfNull(item, nameof(item));
        IEnumerator<T> enumerator = source.GetEnumerator();
        enumerator.MoveNext();
        index = 0;
        do
        {
            if (object.ReferenceEquals(enumerator.Current, item)) return true;
            index++;
        }
        while (enumerator.MoveNext());
        index = -1;
        return false;
    }
}