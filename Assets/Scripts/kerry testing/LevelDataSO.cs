using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[CreateAssetMenu(fileName = "LevelData", menuName = "ScriptableObjects/Level Data")]
public class LevelDataSO : ScriptableObject {

    [BoxGroup("Grid Settings")]
    [SerializeField, MinValue(1)]
    public Vector2Int GridSize = new Vector2Int(5, 5);

    public Vector2Int GoalCoord = new Vector2Int();

    [BoxGroup("Blocks")]
    [ListDrawerSettings(ShowFoldout = true)]
    [SerializeField]
    private List<BlockData> blocks = new List<BlockData>();
    public List<BlockData> Blocks => blocks;
}

[System.Serializable]
public class BlockData {
    [BoxGroup]
    [SerializeField]
    public Vector2Int gridCoord;

    public GameObject blockTypeFab;

    public BlockBehaviour.BlockMoveState pathMode;
    public List<BlockBehaviour.Direction> movePath;

    public bool startFrozen;

    //[BoxGroup]
    //[SerializeField, InlineEditor]
    //public BlockStateSO blockType;

    public BlockData(GameObject blockSourceFab, Vector2Int coord = new Vector2Int()) {
        blockTypeFab = blockSourceFab;
        gridCoord = coord;
    }

}
