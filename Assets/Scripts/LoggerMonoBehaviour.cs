using UnityEngine;

public class LoggerMonoBehaviour : MonoBehaviour {
    [SerializeField] private bool logging = true;

    protected void Log(object message) {
        if (logging) {
            Debug.Log($"[{GetType().Name}] {message}");
        }
    }

    protected void Log(object message, Object context) {
        if (logging) {
            Debug.Log($"[{GetType().Name}] {message}", context);
        }
    }

    protected void LogWarning(object message) {
        if (logging) {
            Debug.LogWarning($"[{GetType().Name}] {message}");
        }
    }

    protected void LogWarning(object message, Object context) {
        if (logging) {
            Debug.LogWarning($"[{GetType().Name}] {message}", context);
        }
    }

    protected void LogError(object message) {
        if (logging) {
            Debug.LogError($"[{GetType().Name}] {message}");
        }
    }

    protected void LogError(object message, Object context) {
        if (logging) {
            Debug.LogError($"[{GetType().Name}] {message}", context);
        }
    }

}
