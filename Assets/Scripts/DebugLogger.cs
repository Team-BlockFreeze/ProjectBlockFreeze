using UnityEngine;

public static class DebugLoggerExtensions {
    private static bool enableLogging = true;

    public static void SetLogging(bool enable) => enableLogging = enable;

    // Static methods that will be accessible globally
    public static void Log(object message) {
        if (enableLogging) Debug.Log(message);
    }

    public static void LogWarning(object message) {
        if (enableLogging) Debug.LogWarning(message);
    }

    public static void LogError(object message) {
        if (enableLogging) Debug.LogError(message);
    }
}
