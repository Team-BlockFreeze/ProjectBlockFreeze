using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadedFunctions : MonoBehaviour
{
    private void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (scene.name == "Level Select Blocks") LevelSelectLoaded();
    }

    private void LevelSelectLoaded() {
        Debug.Log("Camera orthographicSize = 6 on LevelSelect loaded");
        Camera.main.orthographicSize = 6;
    }
}