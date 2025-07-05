using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Systems.SceneManagement {
    public class SceneGroupManager {
        public event Action<string> OnSceneLoaded = delegate { };
        public event Action<string> OnSceneUnloaded = delegate { };
        public event Action OnSceneGroupLoaded = delegate { };

        SceneGroup ActiveSceneGroup;

        private readonly MonoBehaviour _coroutineRunner;

        public SceneGroupManager(MonoBehaviour coroutineRunner) {
            if (coroutineRunner == null) {
                throw new ArgumentNullException(nameof(coroutineRunner), "SceneGroupManager requires a MonoBehaviour instance to run coroutines.");
            }
            _coroutineRunner = coroutineRunner;
        }

        public async Task LoadScenes(SceneGroup group, IProgress<float> progress, bool reloadDupScenes = false) {
            ActiveSceneGroup = group;
            var loadedScenes = new List<string>();

            try {
                await UnloadScenes();
            }
            catch (Exception e) {
                Debug.LogError(e);
            }

            int sceneCount = SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++) {
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);
            }

            var scenesToLoad = ActiveSceneGroup.Scenes
                .Where(sceneData => reloadDupScenes || !loadedScenes.Contains(sceneData.Name))
                .ToList();

            if (scenesToLoad.Count == 0) {
                progress?.Report(1f);
                OnSceneGroupLoaded.Invoke();
                return;
            }

            var operationGroup = new AsyncOperationGroup(scenesToLoad.Count);

            foreach (var sceneData in scenesToLoad) {
                var operation = SceneManager.LoadSceneAsync(sceneData.Reference.Path, LoadSceneMode.Additive);
                operationGroup.Operations.Add(operation);

                await RunCoroutine(Wait(2.5f));

                OnSceneLoaded.Invoke(sceneData.Name);
            }

            await RunCoroutine(WaitForOperations(operationGroup, progress));

            Scene activeScene = SceneManager.GetSceneByName(ActiveSceneGroup.FindSceneNameByType(SceneType.ActiveScene));
            if (activeScene.IsValid()) {
                SceneManager.SetActiveScene(activeScene);
            }

            OnSceneGroupLoaded.Invoke();
        }

        public async Task UnloadScenes() {
            var scenesToUnload = new List<string>();
            var activeSceneName = SceneManager.GetActiveScene().name;

            for (var i = SceneManager.sceneCount - 1; i >= 0; i--) {
                var sceneAt = SceneManager.GetSceneAt(i);
                if (!sceneAt.isLoaded) continue;

                var sceneName = sceneAt.name;
                if (sceneName.Equals(activeSceneName) || sceneName == "Bootstrapper") continue;

                scenesToUnload.Add(sceneName);
            }

            if (scenesToUnload.Count == 0) return;

            var operationGroup = new AsyncOperationGroup(scenesToUnload.Count);

            foreach (var sceneName in scenesToUnload) {
                var operation = SceneManager.UnloadSceneAsync(sceneName);
                if (operation == null) continue;

                operationGroup.Operations.Add(operation);
                OnSceneUnloaded.Invoke(sceneName);
            }

            if (operationGroup.Operations.Count > 0) {
                await RunCoroutine(WaitForOperations(operationGroup, null));
            }

            await Resources.UnloadUnusedAssets();
        }

        private IEnumerator Wait(float seconds) {
            yield return new WaitForSeconds(seconds);
        }

        private IEnumerator WaitForOperations(AsyncOperationGroup operationGroup, IProgress<float> progress) {
            while (!operationGroup.IsDone) {
                progress?.Report(operationGroup.Progress);
                yield return null; // Wait for the next frame
            }
            // Report final progress
            progress?.Report(operationGroup.Progress);
        }

        private Task RunCoroutine(IEnumerator coroutine) {
            var tcs = new TaskCompletionSource<object>();
            _coroutineRunner.StartCoroutine(CoroutineWrapper(coroutine, tcs));
            return tcs.Task;
        }

        private IEnumerator CoroutineWrapper(IEnumerator coroutine, TaskCompletionSource<object> tcs) {
            yield return _coroutineRunner.StartCoroutine(coroutine);
            tcs.SetResult(null);
        }
    }

    public readonly struct AsyncOperationGroup {
        public readonly List<AsyncOperation> Operations;

        public float Progress => Operations.Count == 0 ? 1 : Operations.Average(o => o.progress);
        public bool IsDone => Operations.All(o => o.isDone);

        public AsyncOperationGroup(int initialCapacity) {
            Operations = new List<AsyncOperation>(initialCapacity);
        }
    }
}