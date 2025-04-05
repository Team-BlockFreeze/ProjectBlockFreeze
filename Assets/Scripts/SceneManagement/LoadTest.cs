using System;
using System.Collections.Generic;
using Eflatun.SceneReference;
using Sirenix.OdinInspector;
using Systems.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadTest : MonoBehaviour {
#if UNITY_EDITOR
    [FolderPath(AbsolutePath = false)]
    [SerializeField] private string scanFolderPath = "Assets/Scenes/Levels";

    [InfoBox("Kerry: For some reason the button attribute doesn't work in SceneLoader so i'm just putting it in here")]
    [Button("Auto-Generate SceneGroups From Folder")]
    private void GenerateSceneGroupsFromFolder() {
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { scanFolderPath });

        var existingGroups = new List<SceneGroup>(GetComponent<SceneLoader>().sceneGroups ?? Array.Empty<SceneGroup>());

        foreach (string guid in sceneGuids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            if (sceneAsset == null) {
                Debug.LogWarning($"Could not load scene at path: {path}");
                continue;
            }

            SceneReference sceneRef;
            try {
                sceneRef = new SceneReference(sceneAsset);
            }
            catch (Exception e) {
                Debug.LogError($"Failed to create SceneReference for {path}: {e.Message}");
                continue;
            }

            var sceneData = new SceneData {
                Reference = sceneRef,
                SceneType = SceneType.InactiveScene
            };

            var newGroup = new SceneGroup {
                GroupName = sceneAsset.name,
                Scenes = new List<SceneData> { sceneData }
            };

            existingGroups.Add(newGroup);
        }

        GetComponent<SceneLoader>().sceneGroups = existingGroups.ToArray();

        Debug.Log($"Added {sceneGuids.Length} scene groups from folder: {scanFolderPath}. Total groups: {existingGroups.Count}");
    }
#endif


}