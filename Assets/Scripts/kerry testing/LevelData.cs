using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "LevelData", menuName = "ScriptableObjects/Level Data")]
public class LevelData : ScriptableObject {

    [BoxGroup("Grid Settings")]
    [SerializeField, MinValue(1)]
    private Vector2Int gridSize = new Vector2Int(5, 5);
    public Vector2Int GridSize => gridSize;

    [BoxGroup("Blocks")]
    [ListDrawerSettings(ShowFoldout = true)]
    [SerializeField]
    private List<BlockPlacement> blocks = new List<BlockPlacement>();
    public List<BlockPlacement> Blocks => blocks;
}

[System.Serializable]
public class BlockPlacement {
    [BoxGroup]
    [SerializeField]
    public Vector2Int position;

    [BoxGroup]
    [SerializeField, InlineEditor]
    public BlockStateSO blockType;

}
