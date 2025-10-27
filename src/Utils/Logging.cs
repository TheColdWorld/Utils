using System;
using System.Collections.Generic;
using System.Text;

namespace TheColdWorld.Utils;
/// <summary>
/// A class configure the inner logging of <c>TheColdWorld.Utils</c>
/// </summary>
public static class Logging
{
    private static Action<LogLevel, string>? _log = null;
    /// <summary>
    /// Override the logger of <c>TheColdWorld.Utils</c>
    /// </summary>
    public static void SetLogger(Action<LogLevel, string> logger) =>_log=logger;
	internal static void Log(LogLevel level,string Message)
	{
		if(_log is not null)_log(level, "[TheColdWorld.Utils]"+Message);
	}
	internal static void Log(LogLevel level,string MessagePrefix,Exception exception)
	{
		if(_log is not null)
		{
			StringBuilder sb= new();
            sb.AppendLine($"[TheColdWorld.Utils]{MessagePrefix}.{exception.GetType().FullName}: {exception.Message}");
            sb.AppendLine(exception.StackTrace);
            _log(level,sb.ToString());
		}
	}
	public enum LogLevel
	{
		Debug,
		Infomation,
		Warning, 
		Error
	}
}
