using System.Runtime.CompilerServices;
using System.Text;

namespace TheColdWorld.Utils;
/// <summary>
/// A class configure the inner logging of <c>TheColdWorld.Utils</c>
/// </summary>
public static class Logging
{
    private static object _lock = new();
    public static event  LogAction? OnLogging;
    public  delegate void LogAction(LogLevel level,DateTime logTime,string Message,string? threadname=null,Exception? exception=null);
    internal static void Log(LogLevel level, string Message, Exception? exception=null)
    {
        DateTime current= DateTime.Now;
        string? ThreadName=System.Threading.Thread.CurrentThread.Name;
        Task.Run(() =>
        {
            lock (_lock) {
                OnLogging?.Invoke(level,current,Message,ThreadName,exception);
            }
        });
    }
    public enum LogLevel
    {
        Debug,
        Infomation,
        Warning,
        Error
    }
}
