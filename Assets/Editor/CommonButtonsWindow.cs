using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class CommonButtonsWindow : OdinEditorWindow {
    [MenuItem("Window/Common Buttons")]
    private static void OpenWindow() {
        GetWindow<CommonButtonsWindow>().Show();
    }

    [BoxGroup("Scene Management")]
    [Button("Bootstrapper")]
    private void OpenBootstrapper() {
        OpenScene("Assets/Scenes/Bootstrapper.unity");
    }

    [BoxGroup("Scene Management")]
    [Button("Menu")]
    private void OpenMenu() {
        OpenScene("Assets/Scenes/Menu.unity");
    }

    [BoxGroup("Scene Management")]
    [Button("Level Select Blocks")]
    private void OpenLevelSelectBlocks() {
        OpenScene("Assets/Scenes/Level Select Blocks.unity");
    }

    [BoxGroup("Scene Management")]
    [Button("Empty Grid Level")]
    private void OpenEmptyGridLevel() {
        OpenScene("Assets/Scenes/Empty Grid Level.unity");
    }

    [BoxGroup("Scene Management")]
    [Button("Kerry's Scene for level creating")]
    private void OpenKerrysScene() {
        OpenScene("Assets/Scenes/Kerry's Scene for level creating.unity");
    }

    [BoxGroup("Block Grid")]
    [Button("Load State from SO", ButtonSizes.Medium)]
    private void LoadStateFromSO() {
        BlockGrid.Instance.SetLevelData(levelDataToLoad);
        BlockGrid.Instance.LoadStateFromSO();
    }

    [OnValueChanged("LoadStateFromSO")]
    [BoxGroup("Block Grid")]
    [SerializeField] private LevelDataSO levelDataToLoad;

    [Button("Level Editor")]
    private void OpenLevelEditor() {
        LevelEditorWindow.ShowWindow();
    }

    private void OpenScene(string path) {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) {
            EditorSceneManager.OpenScene(path);
        }
    }



}
