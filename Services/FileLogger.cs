using System;
using System.IO;

namespace SOE_PubEditor.Services;

/// <summary>
/// Simple file logger for debugging on platforms without console access.
/// Logs are written next to the executable for easy access.
/// </summary>
public static class FileLogger
{
    private static readonly object _lock = new();
    private static string? _logPath;
    private static bool _initialized = false;
    
    public static string LogPath
    {
        get
        {
            if (_logPath == null)
            {
                // Write logs next to the executable for easy access
                // Use AppContext.BaseDirectory which works in single-file apps
                var exeDir = AppContext.BaseDirectory;
                var logDir = Path.Combine(exeDir, "Logs");
                Directory.CreateDirectory(logDir);
                _logPath = Path.Combine(logDir, $"pubeditor_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            }
            return _logPath;
        }
    }
    
    public static void Initialize()
    {
        if (!_initialized)
        {
            _initialized = true;
            LogInfo($"FileLogger initialized. Log file: {LogPath}");
            LogInfo($"Application started at {DateTime.Now}");
            LogInfo($"OS: {Environment.OSVersion}");
        }
    }
    
    public static void Log(string message)
    {
        lock (_lock)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var line = $"[{timestamp}] {message}";
                File.AppendAllText(LogPath, line + Environment.NewLine);
                Console.WriteLine(line); // Also write to console if available
            }
            catch (Exception ex)
            {
                // Try to write to a fallback location
                try
                {
                    var fallbackPath = Path.Combine(Environment.CurrentDirectory, "pubeditor_error.log");
                    File.AppendAllText(fallbackPath, $"Logging error: {ex.Message}\n");
                }
                catch { }
            }
        }
    }
    
    public static void LogError(string message, Exception? ex = null)
    {
        var errorMsg = ex != null ? $"{message}: {ex.Message}\n{ex.StackTrace}" : message;
        Log($"ERROR: {errorMsg}");
    }
    
    public static void LogInfo(string message) => Log($"INFO: {message}");
    public static void LogDebug(string message) => Log($"DEBUG: {message}");
    public static void LogWarning(string message) => Log($"WARN: {message}");
}
