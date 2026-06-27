using System;
using System.IO;
using System.Text;

namespace GHAITranslator.Core;

/// <summary>
/// Minimal file logger. No external dependency. <see cref="Warn"/> logs are
/// always written; <see cref="Info"/> is silent in production to avoid disk
/// spam. <see cref="Error"/> writes with a stack trace.
/// </summary>
public static class Log
{
    private static readonly object _gate = new();
    private static readonly StringBuilder _buffer = new();
    private const int MaxBufferedLines = 500;

    public static void Info(string msg)  => Append("INFO",  msg, null);
    public static void Warn(string msg)  => Append("WARN",  msg, null);
    public static void Error(string msg, Exception? ex = null) => Append("ERROR", msg, ex);

    private static void Append(string level, string msg, Exception? ex)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {msg}";
        if (ex != null) line += $"\n{ex}";

        lock (_gate)
        {
            _buffer.AppendLine(line);
            if (_buffer.Length > 80_000)
            {
                // Trim to last half to bound disk usage.
                _buffer.Remove(0, _buffer.Length / 2);
            }
            TryFlush();
        }
    }

    private static void TryFlush()
    {
        try
        {
            Directory.CreateDirectory(PluginPaths.UserDataDir);
            File.WriteAllText(PluginPaths.LogFile, _buffer.ToString());
        }
        catch
        {
            // Logging must never crash the plugin.
        }
    }
}