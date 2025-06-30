using UnityEngine;

public class ExitApplication : MonoBehaviour {
    public void ExitButton() {
        Debug.Log("Exiting Application");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
