using UnityEngine;
using System.Collections;

/// <summary>
/// Used internally for logging.
/// </summary>
public static class SwrveLog
{
    /// <summary>
    /// Set to false to disable the logging produced
    /// by the Swrve SDK.
    /// </summary>

    public static LogLevel Level = LogLevel.Verbose; // Verbose by default

    public enum LogLevel
    {
        Verbose,
        Info,
        Warning,
        Error,
        Disabled
    };

    public delegate void SwrveLogEventHandler(LogLevel level, object message, string tag);

    public static event SwrveLogEventHandler OnLog;

    // Default tag is "activity"
    public static void Log(object message)
    {
        Log(message, "activity");
    }

    public static void LogInfo(object message)
    {
        LogInfo(message, "activity");
    }

    public static void LogWarning(object message)
    {
        LogWarning(message, "activity");
    }

    public static void LogError(object message)
    {
        LogError(message, "activity");
    }

    public static void Log(object message, string tag)
    {
        if (Level == LogLevel.Verbose)
        {
            Debug.Log(message);
            if (OnLog != null)
            {
                OnLog(LogLevel.Verbose, message, tag);
            }
        }
    }

    public static void LogInfo(object message, string tag)
    {
        if (Level == LogLevel.Verbose || Level == LogLevel.Info)
        {
            Debug.Log(message);
            if (OnLog != null)
            {
                OnLog(LogLevel.Info, message, tag);
            }
        }
    }

    public static void LogWarning(object message, string tag)
    {
        if (Level == LogLevel.Verbose || Level == LogLevel.Info || Level == LogLevel.Warning)
        {
            Debug.LogWarning(message);
            if (OnLog != null)
            {
                OnLog(LogLevel.Warning, message, tag);
            }
        }
    }

    public static void LogError(object message, string tag)
    {
        if (Level == LogLevel.Verbose || Level == LogLevel.Info || Level == LogLevel.Warning || Level == LogLevel.Error)
        {
            Debug.LogError(message);
            if (OnLog != null)
            {
                OnLog(LogLevel.Error, message, tag);
            }
        }
    }
}
