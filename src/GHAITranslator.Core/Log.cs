using System;

namespace GHAITranslator.Core;

/// <summary>
/// Lightweight logger that writes to %AppData%\...\plugin.log. We avoid pulling
/// in a third-party logging framework for the P0 milestone — the host
/// (Rhino) already has its own diagnostics, so this is best-effort.
/// </summary>
public static class Log
{
    private static readonly object _gate = new();
    private static string? _path;

    public static void Bind(string logFilePath)
    {
        _path = logFilePath;
        try
        {
            var dir = System.IO.Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
        }
        catch
        {
            _path = null;
        }
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message, Exception? ex = null)
        => Write("ERROR", ex == null ? message : $"{message} | {ex.GetType().Name}: {ex.Message}");

    private static void Write(string level, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        System.Diagnostics.Debug.WriteLine(line);

        lock (_gate)
        {
            if (_path == null) return;
            try
            {
                System.IO.File.AppendAllText(_path, line + Environment.NewLine);
            }
            catch
            {
                // Logging must never throw.
            }
        }
    }
}
