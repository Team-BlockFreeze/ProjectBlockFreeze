using System;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.SceneManagement {
    public class SceneLoader : PersistentSingleton<SceneLoader> {
        [SerializeField] Image loadingBar;
        [SerializeField] float fillSpeed = 0.5f;
        [SerializeField] Canvas loadingCanvas;
        // [SerializeField] Camera loadingCamera;
        [SerializeField] public SceneGroup[] sceneGroups;

        float targetProgress;
        bool isLoading;

        public readonly SceneGroupManager manager = new SceneGroupManager();

        protected override void Awake() {
            base.Awake();

            // TODO can remove
            manager.OnSceneLoaded += sceneName => Log("Loaded: " + sceneName);
            manager.OnSceneUnloaded += sceneName => Log("Unloaded: " + sceneName);
            manager.OnSceneGroupLoaded += () => Log("Scene group loaded");
        }


        // Default LoadSceneGroup 1 on init
        async void Start() {
            await LoadSceneGroupAsync(0);
        }

        void Update() {
            if (!isLoading) return;

            float currentFillAmount = loadingBar.fillAmount;
            float progressDifference = Mathf.Abs(currentFillAmount - targetProgress);

            float dynamicFillSpeed = progressDifference * fillSpeed;

            loadingBar.fillAmount = Mathf.Lerp(currentFillAmount, targetProgress, Time.deltaTime * dynamicFillSpeed);
        }


        /// <summary>
        /// Loads the scene group with the given name after a specified delay in seconds.
        /// </summary>
        /// <param name="groupName">The name of the scene group to load.</param>
        /// <param name="delayInSeconds">The delay in seconds before loading.</param>

        public async void LoadSceneGroup(string groupName, float delayInSeconds) {
            await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));

            int index = Array.FindIndex(sceneGroups, group => group.GroupName == groupName);

            if (index == -1) {
                LogError("Scene group with name " + groupName + " not found.");
                return;
            }

            await LoadSceneGroupAsync(index);
        }



        /// <summary>
        /// Loads a scene group asynchronously (as defined in persistant SceneLoader singleton)
        /// </summary>
        /// <param name="index">Index of the scene group to load</param>
        /// <param name="delayInSeconds">Delay in seconds before loading the scene group</param>
        /// <remarks>
        /// The scene group is found using <see cref="Array.FindIndex{T}"/>,
        /// so the order of the scene groups in the inspector is important.
        /// </remarks>
        public async void LoadSceneGroup(int index, float delayInSeconds) {
            await Task.Delay(TimeSpan.FromSeconds(delayInSeconds));
            await LoadSceneGroupAsync(index);

            Debug.Log("test");
        }

        public async Task LoadSceneGroupAsync(int index) {
            loadingBar.fillAmount = 0f;

            targetProgress = 1f;

            if (index < 0 || index >= sceneGroups.Length) {

                LogError("Invalid scene group index: " + index);
                return;
            }

            LoadingProgress progress = new LoadingProgress();
            progress.Progressed += target => targetProgress = Mathf.Max(target, targetProgress);

            EnableLoadingCanvas();
            await manager.LoadScenes(sceneGroups[index], progress);
            EnableLoadingCanvas(false);
        }

        void EnableLoadingCanvas(bool enable = true) {
            isLoading = enable;
            loadingCanvas?.gameObject.SetActive(enable);

            // loadingCamera?.gameObject.SetActive(enable);
        }

    }

    public class LoadingProgress : IProgress<float> {
        public event Action<float> Progressed;

        const float ratio = 1f;

        public void Report(float value) {
            Progressed?.Invoke(value / ratio);
        }
    }
}