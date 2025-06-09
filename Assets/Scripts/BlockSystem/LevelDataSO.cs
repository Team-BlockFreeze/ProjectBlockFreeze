using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using static BlockBehaviour;

[CreateAssetMenu(fileName = "LevelData", menuName = "ScriptableObjects/Level Data")]
public class LevelDataSO : ScriptableObject {
    [BoxGroup("Level Settings")] public bool autoplayOnStart;

    [ShowIf("autoplayOnStart"), UnityEngine.Range(0.5f, 5f)] public float autoPlayOnStartDelay;

    [BoxGroup("Grid Settings")]
    [SerializeField]
    [MinValue(1)]
    public Vector2Int GridSize = new(5, 5);

    public Vector2Int GoalCoord;

    [BoxGroup("Blocks")]
    [ListDrawerSettings(ShowFoldout = true)]
    [SerializeField]
    private List<BlockData> blocks = new();

    public List<BlockData> Blocks => blocks;
}

[Serializable]
public class BlockData {
    [BoxGroup][SerializeField] public Vector2Int gridCoord;

    public GameObject blockTypeFab;

    public BlockBehaviour.BlockMoveState pathMode;

    public bool canBeFrozen = true; // Default true
    public List<BlockBehaviour.Direction> movePath = new() { BlockBehaviour.Direction.wait };

    public bool startFrozen;

    //[BoxGroup]
    //[SerializeField, InlineEditor]
    //public BlockStateSO blockType;

    public BlockData(GameObject blockSourceFab, Vector2Int coord = new()) {
        blockTypeFab = blockSourceFab;
        gridCoord = coord;
    }


    /// <summary>
    /// Returns string representing blockType. Eg: "path_loop_static" regular block, loops, can't be frozen
    /// </summary>
    public string GetBlockType() {
        if (blockTypeFab == null)
            return null;




        string name = blockTypeFab.name;

        // Check outer types
        if (name.Contains("Wall", StringComparison.OrdinalIgnoreCase))
            return "wall";
        if (name.Contains("Goal", StringComparison.OrdinalIgnoreCase))
            return "goal";


        string type = "";


        if (name.Contains("Key", StringComparison.OrdinalIgnoreCase)) {
            type += "key";
        }
        else if (name.Contains("Path", StringComparison.OrdinalIgnoreCase)) {
            // Path is my name for "regular block". 
            // Example output: "path_loop_static" -> regular block, loops, can't be frozen

            type += "path";

            // Check movement types
            if (pathMode == BlockMoveState.loop) type += "_loop";
            else if (pathMode == BlockMoveState.pingpong) type += "_pingpong";
            else if (pathMode == BlockMoveState.teleport) type += "_teleport";

            // Check if frozen
            if (canBeFrozen == false) {
                type += "_static";
            }
        }
        // Check types based on inner parameters
        if (name.Contains("Path", StringComparison.OrdinalIgnoreCase)) {

        }

        if (type == "") Debug.LogWarning($"Block: {name} has no type.");

        return type;
    }

}