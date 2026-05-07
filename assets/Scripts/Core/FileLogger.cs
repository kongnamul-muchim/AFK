using UnityEngine;
using System.IO;

public static class FileLogger
{
    private static string _logDir;
    private static string _logPath;
    private static bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        _logDir = Path.Combine(Application.persistentDataPath, "Logs", System.DateTime.Now.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(_logDir);

        _logPath = Path.Combine(_logDir, $"session_{System.DateTime.Now:HHmmss}.log");

        Application.logMessageReceived += OnLogMessage;
        Debug.Log($"[FileLogger] Log path: {_logPath}");
    }

    private static void OnLogMessage(string message, string stackTrace, LogType type)
    {
        try
        {
            string prefix = type switch
            {
                LogType.Error or LogType.Exception or LogType.Assert => "[ERROR]",
                LogType.Warning => "[WARN]",
                _ => "[INFO]"
            };

            string line = $"{System.DateTime.Now:HH:mm:ss} {prefix} {message}";
            if (type == LogType.Error || type == LogType.Exception)
                line += $"\n{stackTrace}";

            File.AppendAllText(_logPath, line + "\n");
        }
        catch { }
    }
}
