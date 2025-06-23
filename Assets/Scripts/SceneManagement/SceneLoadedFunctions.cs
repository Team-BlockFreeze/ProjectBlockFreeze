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
    }

    private void LevelSelectLoaded() {
        Camera.main.orthographicSize = 10;
        Debug.Log($"Camera orthographicSize = {Camera.main.orthographicSize} on LevelSelect loaded");

    }
}