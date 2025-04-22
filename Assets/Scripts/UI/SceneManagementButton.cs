using Systems.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagementButton : MonoBehaviour
{
    private const int baseLevelIdx = 2;

    public void ReloadScene() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadNextScene() {
        var i = SceneManager.GetActiveScene().buildIndex + 1;
        if (i >= SceneManager.sceneCountInBuildSettings) {
            Debug.LogError($"Tried to load level at build index {i}, out of bounds");
            return;
        }

        Time.timeScale = 1;
        SceneManager.LoadScene(i);
    }

    public void LoadAsyncSceneGroupByIdx(int idx) {
        SceneLoader.Instance.LoadSceneGroup(idx, 0f);
    }

    public void LoadAsynceNextLevel() {
        LevelAreaController.Instance.LoadNextLevel();
    }
}