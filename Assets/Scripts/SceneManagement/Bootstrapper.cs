using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrapper : PersistentSingleton<Bootstrapper> {
    private static bool initializeOnLoad = false;



    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static async void Init() {
        if (!initializeOnLoad) {
            Debug.Log("Bootstrapper Init skipped due to flag.");
            return;
        }

        Debug.Log("Loaded Bootstrapper...");
        await SceneManager.LoadSceneAsync("Bootstrapper", LoadSceneMode.Single);
    }
}