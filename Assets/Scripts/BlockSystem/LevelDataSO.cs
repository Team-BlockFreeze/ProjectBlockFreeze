using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "ScriptableObjects/Level Data")]
public class LevelDataSO : ScriptableObject {
    [BoxGroup("Level Settings")] public bool autoplayOnStart;

    [ShowIf("autoplayOnStart"), Range(0.5f, 5f)] public float autoPlayOnStartDelay;

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
    public List<BlockBehaviour.Direction> movePath = new() { BlockBehaviour.Direction.wait };

    public bool startFrozen;

    //[BoxGroup]
    //[SerializeField, InlineEditor]
    //public BlockStateSO blockType;

    public BlockData(GameObject blockSourceFab, Vector2Int coord = new()) {
        blockTypeFab = blockSourceFab;
        gridCoord = coord;
    }
}