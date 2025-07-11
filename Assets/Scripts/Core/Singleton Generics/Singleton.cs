using UnityEngine;

namespace UnityUtils {

    //! SIngleton that doesn't persist between scenes
    //! Only exists in one scene
    public class Singleton<T> : LoggerMonoBehaviour where T : Component {
        protected static T instance;

        public static bool HasInstance => instance != null;
        public static T TryGetInstance() => HasInstance ? instance : null;

        public static T Instance {
            get {
                if (instance == null) {
                    instance = FindAnyObjectByType<T>();
                    if (instance == null) {
                        Debug.LogWarning($"Singleton<{typeof(T).Name}> not found in scene");
                        // var go = new GameObject(typeof(T).Name + " Auto-Generated");
                        // instance = go.AddComponent<T>();
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Make sure to call base.Awake() in override if you need awake.
        /// </summary>
        protected virtual void Awake() {
            InitializeSingleton();
        }

        protected virtual void InitializeSingleton() {
            if (!Application.isPlaying) return;

            instance = this as T;
        }
    }
}