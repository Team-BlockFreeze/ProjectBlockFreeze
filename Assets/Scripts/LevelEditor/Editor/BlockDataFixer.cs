using UnityEditor;
using UnityEngine;

public static class BlockDataFixer {

    /// <summary>
    /// Every time you add a parameter to the level editor, it'll fuck shit up. 
    /// Use this utility to mass modify parameters on all levels.
    /// 
    /// How to use:
    /// 1. git commit your existing changes
    /// 2. Modify the "CODE BLOCK" below to your needs.
    /// 3. Make VERY fucking sure you mass modify the correct blockType (see BlockData.GetBlockType() for reference)
    /// 4. Run the method from the Tools menu in Unity.
    /// 5. If shit's fucked, revert to previous commit
    /// </summary>
    [MenuItem("Tools/Fix BlockData canBeFrozen Defaults")]
    public static void FixCanBeFrozenDefaults() {
        string[] guids = AssetDatabase.FindAssets("t:LevelDataSO");

        int totalFixed = 0;

        foreach (string guid in guids) {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LevelDataSO asset = AssetDatabase.LoadAssetAtPath<LevelDataSO>(path);
            bool assetModified = false;

            // For every block in every LevelDataSO
            foreach (var block in asset.Blocks) {

                #region CODE BLOCK

                if (block.GetBlockType() == "wall") continue;

                if (block.canBeFrozen == false) {
                    block.canBeFrozen = true;



                    assetModified = true;
                    totalFixed++;
                }

                #endregion CODE BLOCK

            }

            if (assetModified) {
                EditorUtility.SetDirty(asset);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Fixed {totalFixed} block(s) with missing 'canBeFrozen = true'");
    }
}
