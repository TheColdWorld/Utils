using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public static TResult FirstOrThrow<TResult,TException>(this IEnumerable<TResult> source,Func<TException> exceptionConstructor) where TException : Exception
    {
        var result=source.TryGetFirst(out bool present);
        return !present ? throw exceptionConstructor() : result!;
    }
    /// <summary>
    /// get the first item from <paramref name="source"/>
    /// </summary>
    /// <param name="found"><see langword="true"/> if <paramref name="source"/> is not empty</param>
    /// <returns>First item of <paramref name="source"/> if <paramref name="source"/> is not empty,otherwise <see langword="null"/></returns>
    /// <exception cref="ArgumentNullException">if <paramref name="source"/> is <see langword="null"/></exception>
    public static TResult? TryGetFirst<TResult>(this IEnumerable<TResult> source,out bool found)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (source is IList<TResult> list)
        {
            if(list.Count >0)
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
    public static TResult LastOrThrow<TResult,TException>(this IEnumerable<TResult> source,Func<TException> exceptionConstucter)where TException : Exception
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
            if(enumerator.MoveNext())
            {
                TResult result=enumerator.Current;
                while (enumerator.MoveNext()) result= enumerator.Current;
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
    /// <returns></returns>
    public static TResult FirstOrDefault<TResult>(this IEnumerable<TResult> source,Func<TResult> orElseGetter)
    {
        var rst=source.TryGetFirst(out bool present);
        return !present ? orElseGetter() : rst!;
    }
    /// <summary>
    /// get the last item from <paramref name="source"/> or the <see langword="value"/> of <paramref name="orElseGetter"/>
    /// </summary>
    /// <param name="orElseGetter">The getter if <paramref name="source"/> is empty</param>
    /// <returns></returns>
    public static TResult LastOrDefault<TResult>(this IEnumerable<TResult> source, Func<TResult> orElseGetter)
    {
        var rst = source.TryGetLast(out bool present);
        return !present ? orElseGetter() : rst!;
    }
}
