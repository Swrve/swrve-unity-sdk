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
    public static bool Verbose = true;

    public enum SwrveLogType {
        Info,
        Warning,
        Error
    };

    public delegate void SwrveLogEventHandler (SwrveLog.SwrveLogType type,object message,string tag);

    public static event SwrveLogEventHandler OnLog;

    // Default tag is "activity"
    public static void Log (object message)
    {
        Log (message, "activity");
    }

    public static void LogWarning (object message)
    {
        LogWarning (message, "activity");
    }

    public static void LogError (object message)
    {
        LogError (message, "activity");
    }

    public static void Log (object message, string tag)
    {
        if (Verbose) {
            Debug.Log (message);
            if (OnLog != null) {
                OnLog (SwrveLogType.Info, message, tag);
            }
        }
    }

    public static void LogWarning (object message, string tag)
    {
        if (Verbose) {
            Debug.LogWarning (message);
            if (OnLog != null) {
                OnLog (SwrveLogType.Warning, message, tag);
            }
        }
    }

    public static void LogError (object message, string tag)
    {
        Debug.LogError (message);
        if (OnLog != null) {
            OnLog (SwrveLogType.Error, message, tag);
        }
    }
}
