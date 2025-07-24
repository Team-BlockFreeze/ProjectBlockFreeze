using UnityEngine;
using UnityEditor;
using TMPro;
using System.Text.RegularExpressions;

public class FixLevelButtonTexts : MonoBehaviour {
    [MenuItem("Tools/Fix Selected LevelButtonTexts")]
    static void FixLevelNumberTexts() {
        var selected = Selection.gameObjects;

        if (selected.Length == 0) {
            Debug.LogWarning("No GameObjects selected.");
            return;
        }

        int changedCount = 0;
        var dashZeroPattern = new Regex(@"-(0*)(\d+)"); // T-09 -? T9
        var legacySuffixPattern = new Regex(@"_.*$");   // A9_B1 -> A9

        foreach (var obj in selected) {
            var levelButton = obj.GetComponent<LevelButton>();
            if (levelButton == null) {
                Debug.LogWarning($"'{obj.name}' does not have a LevelButton component. Skipping.");
                continue;
            }

            TMP_Text tmp = levelButton.LevelNumberText;
            if (tmp == null) {
                Debug.LogWarning($"'{obj.name}' LevelNumberText is null. Skipping.");
                continue;
            }

            string originalText = tmp.text;
            string cleaned = dashZeroPattern.Replace(originalText, "$2");
            cleaned = legacySuffixPattern.Replace(cleaned, "");
            if (cleaned != originalText) {
                tmp.text = cleaned;
                EditorUtility.SetDirty(tmp);
                Debug.Log($"Updated '{obj.name}': '{originalText}' → '{cleaned}'");
                changedCount++;
            }
        }
    }
}
