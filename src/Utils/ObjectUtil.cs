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
    public static T RequirsNotNull<T>(T? item) => item is null ? throw new ArgumentNullException(nameof(item)) : item;
    /// <typeparam name="T">any class</typeparam>
    /// <param name="item">The reference type argument to validate as non-null.</param>
    /// <param name="Default">the default object if <paramref name="item"/> is <see langword="null"/></param>
    /// <returns><paramref name="item"/> or <paramref name="Default"/> (if <paramref name="item"/> is <see langword="null"/>)</returns>
    public static T RequirsNotNullOrDefault<T>(T? item, [NotNull]T Default) => item is null ? Default : item;
}

