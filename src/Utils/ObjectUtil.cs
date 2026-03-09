using System.Diagnostics.CodeAnalysis;

namespace TheColdWorld.Utils;

/// <summary>
/// Utils of <see langword="object"/> vaildcation
/// </summary>
public static class ObjectUtil
{
    /// <typeparam name="T">any class</typeparam>
    /// <param name="item">The reference type argument to validate as non-null.</param>
    /// <returns><paramref name="item"/></returns>
    /// <exception cref="ArgumentNullException">if item is <see langword="null"/></exception>
    public static T RequiresNotNull<T>(T? item) => item is null ? throw new ArgumentNullException(nameof(item)) : item;
    /// <typeparam name="T">any class</typeparam>
    /// <param name="item">The reference type argument to validate as non-null.</param>
    /// <param name="Default">the default object if <paramref name="item"/> is <see langword="null"/></param>
    /// <returns><paramref name="item"/> or <paramref name="Default"/> (if <paramref name="item"/> is <see langword="null"/>)</returns>
    public static T RequiresNotNullOrDefault<T>(T? item, [NotNull] T Default) => item is null ? Default : item;
    /// <summary>[Backporting from newer version from .net]Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is null.</summary>
    /// <param name="argument">The reference type argument to validate as non-null.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    [DoesNotReturn]
    public static void ThrowIfNull<T>(T? item, [System.Runtime.CompilerServices.CallerMemberName] string paramName = "")
    {
        if (item is null) throw new ArgumentNullException(paramName);
    }
    public static string GetHashCodeHexString(object? obj) => obj is null ? "null" : BitConverter.ToString(BitConverter.GetBytes(obj.GetHashCode())).Replace("-", string.Empty);
    public static string GetHashCodeHexString<T>(T obj) => obj is null ? "null" : BitConverter.ToString(BitConverter.GetBytes(obj.GetHashCode())).Replace("-", string.Empty);
}

