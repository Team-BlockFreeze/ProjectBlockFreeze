using Sirenix.OdinInspector;
using UnityEngine;

public class PersistentSingleton<T> : LoggerMonoBehaviour where T : Component {
    [Title("Persistent Singleton")]
    [Tooltip("if this is true, this singleton will auto detach if it finds itself parented on awake")]
    public bool UnparentOnAwake = true;

    public static bool HasInstance => instance != null;
    public static T Current => instance;

    protected static T instance;

    public static T Instance {
        get {
            if (instance == null) {
                instance = FindFirstObjectByType<T>();
                if (instance == null) {
                    Debug.LogWarning($"Singleton<{typeof(T).Name}> not found in scene");
                    // GameObject obj = new GameObject();
                    // obj.name = typeof(T).Name + "AutoCreated";
                    // instance = obj.AddComponent<T>();
                }
            }

            return instance;
        }
    }

    /// <summary>
    /// Remeber to call base.Awake() in override
    /// </summary>
    protected virtual void Awake() => InitializeSingleton();

    protected virtual void InitializeSingleton() {
        if (!Application.isPlaying) {
            return;
        }

        if (UnparentOnAwake) {
            transform.SetParent(null);
        }

        if (instance == null) {
            instance = this as T;
            DontDestroyOnLoad(transform.gameObject);
            enabled = true;
        }
        else {
            if (this != instance) {
                Destroy(this.gameObject);
            }
        }
    }
}