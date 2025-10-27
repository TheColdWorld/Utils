using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheColdWorld.Utils;
/// <summary>
/// Utils of <see cref="FileInfo"/> and <see cref="DirectoryInfo"/>
/// </summary>
public static class FileUtil
{
    /// <summary>
    /// get the sub file of <paramref name="directory"/> with  <paramref name="name"/>
    /// </summary>
    /// <param name="directory">The directory will be dound</param>
    /// <param name="name">The file name(with prefix) will be found</param>
    /// <returns><see cref="FileInfo"/> of <paramref name="directory"/> / <paramref name="name"/></returns>
    /// <exception cref="FileNotFoundException">if <paramref name="directory"/> / <paramref name="name"/> not exists</exception>
    public static FileInfo SubFileOf(this DirectoryInfo directory, string name) => directory.EnumerateFiles(name, SearchOption.TopDirectoryOnly).FirstOrThrow(() => new FileNotFoundException($"Subfile '{name}' is not found!"));
    /// <summary>
    /// get the sub directory of <paramref name="directory"/> with  <paramref name="name"/>
    /// </summary>
    /// <param name="directory">The directory will be dound</param>
    /// <param name="name">The directory name will be found</param>
    /// <returns><see cref="DirectoryInfo"/> of <paramref name="directory"/> / <paramref name="name"/></returns>
    /// <exception cref="FileNotFoundException">if <paramref name="directory"/> / <paramref name="name"/> not exists</exception>
    public static DirectoryInfo SubDirectoryOf(this DirectoryInfo directory, string name) => directory.EnumerateDirectories(name, SearchOption.TopDirectoryOnly).FirstOrThrow(() => new FileNotFoundException($"Subdir '{name}' is not found!"));
    /// <summary>
    /// get the sub file of <paramref name="directory"/> with  <paramref name="name"/> ,create if not exists
    /// </summary>
    /// <param name="directory">The directory will be dound</param>
    /// <param name="name">The file name(with prefix) will be found or created</param>
    /// <returns><see cref="FileInfo"/> of <paramref name="directory"/> / <paramref name="name"/></returns>
    public static FileInfo GetSubFileOrCreate(this DirectoryInfo directory,string name)=> directory.EnumerateFiles(name).FirstOrDefault(() => {
            using FileStream fs = File.Create(Path.Combine(directory.FullName, name));
            return directory.SubFileOf(name);
        });
    /// <summary>
    /// get the sub directory of <paramref name="directory"/> with  <paramref name="name"/> ,create if not exists
    /// </summary>
    /// <param name="directory">The directory will be dound</param>
    /// <param name="name">The directory name(with prefix) will be found or created</param>
    /// <returns><see cref="DirectoryInfo"/> of <paramref name="directory"/> / <paramref name="name"/></returns>
    public static DirectoryInfo GetSubDirectoryOrCreate(this DirectoryInfo directory, string name)=> directory.EnumerateDirectories(name).FirstOrDefault(() => Directory.CreateDirectory(Path.Combine(directory.FullName, name)));
}
