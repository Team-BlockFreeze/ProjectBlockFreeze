using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadedFunctions : MonoBehaviour {
    private void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (scene.name == "Level Select Blocks") LevelSelectLoaded();
        if (scene.name == "Empty Grid Level") EmptyGridLevelLoaded();
    }

    private void LevelSelectLoaded() {
        Camera.main.orthographicSize = 10;
        Debug.Log($"Camera orthographicSize = {Camera.main.orthographicSize} on LevelSelect loaded");

        foreach (var levelArea in LevelAreaController.Instance.Selectors) {
            foreach (var branchArrowContainer in levelArea.BranchArrowContainer.GetComponentsInChildren<LevelBranchTransition>(true)) {
                if (branchArrowContainer.unlocked) branchArrowContainer.ActivateBranchTransition();
            }
        }

        if (loadedForFirstTime) {
            LevelAreaController.Instance.TransitionToLevelArea(new LevelDataSO.BranchTarget { TargetGroupName = "T", TargetLevelName = "T1" });
            loadedForFirstTime = false;
        }
    }

    private void EmptyGridLevelLoaded() {
        Debug.Log($"Camera orthographicSize = {Camera.main.orthographicSize} on Empty Grid Level loaded");

        foreach (var levelArea in LevelAreaController.Instance.Selectors) {
            levelArea.HideAllButtons();

            foreach (var branchArrowContainer in levelArea.BranchArrowContainer.GetComponentsInChildren<LevelBranchTransition>(true)) {
                branchArrowContainer.DeactivateBranchTransition();

            }
        }

    }

    bool loadedForFirstTime = true;

}