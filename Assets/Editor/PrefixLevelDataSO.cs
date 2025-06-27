using UnityEngine;
using UnityEditor;
using System.IO;

public class PrefixLevelDataSO : EditorWindow {
    private string prefix = "E";

    [MenuItem("Tools/Rename ScriptableObjects/With Prefix in Folder")]
    private static void ShowWindow() {
        GetWindow<PrefixLevelDataSO>("Rename SOs in Folder");
    }

    private void OnGUI() {
        GUILayout.Label("Rename ScriptableObjects in Folder", EditorStyles.boldLabel);

        prefix = EditorGUILayout.TextField("Prefix to Add", prefix);

        if (GUILayout.Button("Rename Assets in Selected Folder")) {
            RenameAssetsInFolder();
        }
    }

    private void RenameAssetsInFolder() {
        var selectedFolder = Selection.activeObject;

        if (selectedFolder == null) {
            Debug.LogError("No folder selected.");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(selectedFolder);

        if (!AssetDatabase.IsValidFolder(folderPath)) {
            Debug.LogError("Selected object is not a folder.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { folderPath });

        int renamedCount = 0;

        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string oldName = Path.GetFileNameWithoutExtension(path);

            if (!oldName.StartsWith(prefix)) {
                string newName = prefix + oldName;
                AssetDatabase.RenameAsset(path, newName);
                renamedCount++;
                Debug.Log($"Renamed: {oldName} ➝ {newName}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Renamed {renamedCount} asset(s) with prefix \"{prefix}\".");
    }
}
