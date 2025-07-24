using UnityEngine;
using UnityEditor;
using TMPro;

public class FixLevelButtonText : MonoBehaviour {
    [MenuItem("Tools/Fix Level Button Texts (Remove -0)")]
    static void FixTexts() {
        var selected = Selection.gameObjects;

        if (selected.Length == 0) {
            Debug.LogWarning("No GameObjects selected.");
            return;
        }

        int changedCount = 0;

        foreach (var obj in selected) {
            var tmps = obj.GetComponentsInChildren<TextMeshProUGUI>(true);

            foreach (var tmp in tmps) {
                if (tmp.text.Contains("-0")) {
                    string oldText = tmp.text;
                    tmp.text = tmp.text.Replace("-0", "-");
                    EditorUtility.SetDirty(tmp);
                    Debug.Log($"Updated: '{oldText}' → '{tmp.text}' on {tmp.gameObject.name}");
                    changedCount++;
                }
            }
        }

        Debug.Log($"Fixed {changedCount} TextMeshProUGUI elements.");
    }
}