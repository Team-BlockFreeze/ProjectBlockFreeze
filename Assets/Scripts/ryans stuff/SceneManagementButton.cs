using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagementButton : MonoBehaviour
{
    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadNextScene()
    {
        int i = SceneManager.GetActiveScene().buildIndex + 1;
        if(i >= SceneManager.sceneCountInBuildSettings) {
            Debug.LogError($"Tried to load level at build index {i}, out of bounds");
            return;
        }

        Time.timeScale = 1;
        SceneManager.LoadScene(i);
    }
}
