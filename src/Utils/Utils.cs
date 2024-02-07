namespace TheColdWorld.Utils;


public static class Utils
{
    /// <typeparam name="T">any class</typeparam>
    /// <param name="item">the object that be ckecked null</param>
    /// <returns>item</returns>
    /// <exception cref="ArgumentNullException">if item is null</exception>
    public static T RequirsNotNull<T>(T? item) => item is null ? throw new ArgumentNullException(nameof(item)) : item;
    /// <typeparam name="T">any class</typeparam>
    /// <param name="item">the object that be ckecked null</param>
    /// <param name="Default">the return object if item is null</param>
    /// <returns>item or Default (if item is null)</returns>
    public static T RequirsNotNullOrDefault<T>(T? item, T Default) => item is null ? Default : item;
}

