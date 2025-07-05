using System;
using System.Collections;
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
        public SceneGroup[] sceneGroups;

        float targetProgress;
        bool isLoading;

        public SceneGroupManager manager;

        protected override void Awake() {
            base.Awake();
            manager = new SceneGroupManager(this);

            manager.OnSceneLoaded += sceneName => Log("Loaded: " + sceneName);
            manager.OnSceneUnloaded += sceneName => Log("Unloaded: " + sceneName);
            manager.OnSceneGroupLoaded += () => Log("Scene group loaded");
        }


        async void Start() {
            await LoadSceneGroupAsync(0);
        }

        void Update() {
            if (!isLoading) return;

            float currentFillAmount = loadingBar.fillAmount;

            loadingBar.fillAmount = Mathf.Lerp(currentFillAmount, targetProgress, Time.deltaTime * fillSpeed);
        }


        /// <summary>
        /// Loads the scene group with the given name after a specified delay in seconds.
        /// </summary>
        /// <param name="groupName">The name of the scene group to load.</param>
        /// <param name="delayInSeconds">The delay in seconds before loading.</param>
        public async void LoadSceneGroup(string groupName, float delayInSeconds) {
            if (delayInSeconds > 0) {
                await WaitAsync(delayInSeconds);
            }

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
            if (delayInSeconds > 0) {
                await WaitAsync(delayInSeconds);
            }
            await LoadSceneGroupAsync(index);
        }

        public async Task LoadSceneGroupAsync(int index) {
            loadingBar.fillAmount = 0f;
            targetProgress = 0f;

            if (index < 0 || index >= sceneGroups.Length) {
                LogError("Invalid scene group index: " + index);
                return;
            }

            LoadingProgress progress = new LoadingProgress();
            progress.Progressed += newProgress => targetProgress = Mathf.Max(newProgress, targetProgress);

            EnableLoadingCanvas();
            await manager.LoadScenes(sceneGroups[index], progress);

            targetProgress = 1f;
            await WaitUntilAsync(() => Mathf.Approximately(loadingBar.fillAmount, 1f));

            EnableLoadingCanvas(false);
        }

        private Task WaitAsync(float seconds) {
            return StartCoroutineAsTask(WaitCoroutine(seconds));
        }

        private Task WaitUntilAsync(Func<bool> predicate) {
            return StartCoroutineAsTask(WaitUntilCoroutine(predicate));
        }

        private IEnumerator WaitCoroutine(float seconds) {
            yield return new WaitForSeconds(seconds);
        }

        private IEnumerator WaitUntilCoroutine(Func<bool> predicate) {
            yield return new WaitUntil(predicate);
        }

        private Task StartCoroutineAsTask(IEnumerator coroutine) {
            var tcs = new TaskCompletionSource<object>();
            StartCoroutine(RunCoroutine(coroutine, tcs));
            return tcs.Task;
        }

        private IEnumerator RunCoroutine(IEnumerator coroutine, TaskCompletionSource<object> tcs) {
            yield return coroutine;
            tcs.SetResult(null);
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