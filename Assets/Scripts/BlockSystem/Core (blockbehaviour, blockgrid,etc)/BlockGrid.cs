using System;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityUtils;

public class BlockGrid : Singleton<BlockGrid> {
    [Title("Grid Settings")]
    [SerializeField]
    [ReadOnly]
    private Vector2Int gridSize;

    [SerializeField] private Vector2Int goalCoord;

    [OnValueChanged("LoadStateFromSO")]
    [SerializeField]
    [InlineEditor]
    private LevelDataSO levelData;
    public void SetLevelData(LevelDataSO data) {
        levelData = data;
    }

    [SerializeField] private bool loadFromLvlSelectOnStart = true;

    [FoldoutGroup("Grid Rendering")]
    [SerializeField]
    private DynamicGridBorder dynamicGridBorder;

    [FoldoutGroup("Grid Rendering")]
    [SerializeField]
    private SpriteRenderer validGridSprite;

    [FoldoutGroup("Grid Rendering")]
    [SerializeField]
    private Transform gridPlaneParentT;

    [FoldoutGroup("Grid Rendering")]
    [SerializeField]
    private MeshRenderer gridPlaneMeshR;

    [FoldoutGroup("Grid Rendering")]
    [SerializeField]
    private SnapToGrid goalBlockScript;

    [FoldoutGroup("Block Materials")]
    [SerializeField]
    public Material frozenMAT;

    [FoldoutGroup("Block Materials")]
    [SerializeField]
    public Material loopMAT;

    [FoldoutGroup("Block Materials")]
    [SerializeField]
    public Material pingpongMAT;

    [FoldoutGroup("Block Materials")]
    [SerializeField]
    public Material wallMAT;

    [FoldoutGroup("Block Materials")]
    [SerializeField]
    public Material keyMAT;

    [FoldoutGroup("Block Materials")]
    [SerializeField]
    public Material key_pingpongMAT;

    [FoldoutGroup("Special Properties Icons")]
    public Sprite nofreezeBlockIcon;

    [FoldoutGroup("Special Properties Icons")]
    public Sprite pushableWhenFrozenIcon;

    [SerializeField] public GridState ActiveGridState;
    public BlockTypesListSO blockTypesList;


    [FormerlySerializedAs("blocksList")]
    [SerializeField]
    private Transform blocksListTransform;

    public Vector2Int GridSize => gridSize;
    public LevelDataSO LevelData => levelData;
    public Vector2Int GoalCoord => goalCoord;


    public static UnityEvent<LevelDataSO> Event_LevelFirstLoad = new UnityEvent<LevelDataSO>();

    private void Start() {
        //gridSize = startGridStateSO.GridSize;
        if (loadFromLvlSelectOnStart) {
            levelData = LevelAreaController.Instance.ChosenLevel;
            LoadStateFromSO();
        }

        Debug.Log("loading new level for first time as new scene");
        Event_LevelFirstLoad?.Invoke(levelData);

        ActiveGridState.GridBlockStates = new BlockBehaviour[gridSize.x, gridSize.y];

        //foreach (var block in ActiveGridState.GridBlockStates) {
        //    if (block == null) continue;

        //    ActiveGridState.BlocksList.Add(block);
        //}

        ReloadGridVisuals();

        BlockCoordinator.Instance?.ManualStart();
    }


#if UNITY_EDITOR
    private void OnDrawGizmos() {
        // Debug.Log(ActiveGridState.BlocksList[5].canBeFrozen);

        //Draw Debug grid lines
        var botLeft = GetBotLeftOriginPos();
        for (var i = 1; i < gridSize.x; i++) {
            var start = botLeft + Vector3.right * i;
            Gizmos.DrawLine(start, start + Vector3.up * gridSize.y);
        }

        for (var i = 1; i < gridSize.y; i++) {
            var start = botLeft + Vector3.up * i;
            Gizmos.DrawLine(start, start + Vector3.right * gridSize.y);
        }
    }
#endif


    public Action StateLoadedFromSO;

    [PropertyOrder(-10)]
    [FoldoutGroup("Actions")]
    [Button(ButtonSizes.Medium)]
    public void LoadStateFromSO() {
        BlockCoordinator.Instance.ClearUndoStack();

        //destroy current blocks

        //! Kerry: commented this out reset active grid already clears the list
        // foreach (var b in ActiveGridState.BlocksList)
        //     if (b != null)
        //         GameObject.DestroyImmediate(b.gameObject);

        //clear active grid state
        ResetActiveGrid();

        //load data from level data SO
        gridSize = levelData.GridSize;
        goalCoord = levelData.GoalCoord;

        ResetActiveGrid();

        dynamicGridBorder.SetGridSize(gridSize, 0.5f);

        //load blocks from level data SO
        foreach (var bData in levelData.Blocks) {

            // Block inits
            var newBlock = Instantiate(bData.blockTypeFab, blocksListTransform).GetComponent<BlockBehaviour>();
            newBlock.transform.position = GetWorldSpaceFromCoord(bData.gridCoord);
            //newBlock.transform.parent
            newBlock.SetGridRef(this);
            newBlock.TryAddToGrid();

            newBlock.moveMode = bData.pathMode;
            newBlock.SetMovePath(bData.movePath?.ToArray());

            // Apply mats
            ColorPalateInjector.Instance.InjectColorsIntoScene();
            ApplyMaterialsToBlocks(newBlock);

            if (bData.startFrozen) newBlock.TrySetFreeze(true);


            if (bData.canBeFrozen == false) newBlock.canBeFrozen = false;
            if (bData.pushableWhenFrozen == true) newBlock.pushableWhenFrozen = true;
            if (bData.phaseThrough == true) newBlock.phaseThrough = true;

            //! If teleport block, update destination
            newBlock.GetComponent<BlockTeleportTile>()?.UpdateTeleportDestination();

            // Animation finishes in 0.5s (see IngameCanvasButtons.ReloadLevel(): moveDuration variablke). dirty fix for updating line renderer after reload animation finishes
            DOVirtual.DelayedCall(GameSettings.Instance.reloadAnimationTime / 2f + 0.1f, () => { // 0.1fs buffer window
                newBlock.GetComponent<BlockTeleportTile>()?.UpdateLineRenderer();
                newBlock.GetComponent<BlockPreview>()?.UpdateLine();
                newBlock.GetComponent<BlockPreview>()?.DrawPath();
            });


            newBlock.blockType = bData.GetBlockType();
            // Debug.Log(newBlock.blockType);
            newBlock.SetBlockTypeIcons(GetIconsForBlockType(newBlock.blockType));

            newBlock.UpdateMovementVisualiser();

            // Debug.Log($"Block: '{bData.GetBlockType()}' at {bData.gridCoord}");
        }


        if (levelData.autoplayOnStart && GameSettings.Instance.IsAutoPlaying == false)

            DOVirtual.DelayedCall(LevelData.autoPlayOnStartDelay,
                () => {
                    BlockCoordinator.Instance.SetAutoplay(false);
                });


        StateLoadedFromSO?.Invoke();
        EditorUtility.SetDirty(this);
    }


    private Dictionary<string, Sprite> GetIconsForBlockType(string blockType) {
        if (blockType.Contains("wall")) return new Dictionary<string, Sprite>();
        Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

        if (blockType.Contains("nofreeze")) {
            Debug.Log(blockType);
            icons.Add("topleft", nofreezeBlockIcon);
        }

        if (blockType.Contains("pushableWhenFrozen")) {
            icons.Add("topright", pushableWhenFrozenIcon);
        }

        return icons;
    }

    private void ApplyMaterialsToBlocks(BlockBehaviour newBlock) {
        if (newBlock == null) return;

        Renderer blockRenderer = newBlock.GetComponentInChildren<MeshRenderer>();
        if (blockRenderer == null) return;

        var isWall = newBlock.gameObject.name.Contains("Block Wall");
        var isKey = newBlock.gameObject.name.Contains("Block Key");

        if (newBlock.frozen && !isWall) {
            blockRenderer.sharedMaterial = frozenMAT;
            newBlock.BlockMaterial = frozenMAT;
            return;
        }

        if (isKey) {
            if (newBlock.moveMode == BlockBehaviour.BlockMoveState.pingpong) {
                blockRenderer.sharedMaterial = key_pingpongMAT;
                newBlock.BlockMaterial = key_pingpongMAT;
                return;
            }
            blockRenderer.sharedMaterial = keyMAT;
            newBlock.BlockMaterial = keyMAT;
            return;
        }

        if (isWall) {
            blockRenderer.sharedMaterial = wallMAT;
            newBlock.BlockMaterial = wallMAT;
            return;
        }

        switch (newBlock.moveMode) {
            case BlockBehaviour.BlockMoveState.pingpong:
                blockRenderer.sharedMaterial = pingpongMAT;
                newBlock.BlockMaterial = pingpongMAT;
                break;

            case BlockBehaviour.BlockMoveState.loop:
                blockRenderer.sharedMaterial = loopMAT;
                newBlock.BlockMaterial = loopMAT;
                break;

            default:
                Debug.LogWarning($"Unhandled block move mode: {newBlock.moveMode} on {newBlock.name}");
                break;
        }
    }

    //[FoldoutGroup("Actions"), Button(ButtonSizes.Medium)]
    //public void SaveStateToSO() { }

    [PropertyOrder(-10)]
    [FoldoutGroup("Actions")]
    [Button(ButtonSizes.Large)]
    public void ResetActiveGrid() {
        while (blocksListTransform.childCount > 0) {
            var child = blocksListTransform.GetChild(0);
            DestroyImmediate(child.gameObject);
        }


        gridSize = levelData.GridSize;
        ActiveGridState.GridBlockStates = new BlockBehaviour[levelData.GridSize.x, levelData.GridSize.y];
        ActiveGridState.BlocksList = new List<BlockBehaviour>();

        ReloadGridVisuals();
    }

    [PropertyOrder(-10)]
    [FoldoutGroup("Actions")]
    [Button(ButtonSizes.Medium)]
    public void ReloadGridVisuals() {
        validGridSprite.size = new Vector2(gridSize.x, gridSize.y);

        var planePosY = gridSize.y % 2 == 0 ? 0 : -0.5f / gridPlaneParentT.localScale.y;
        gridPlaneParentT.position = new Vector3(GetBotLeftOriginPos().x, planePosY, 0);

        var goalWorldPos = GetWorldSpaceFromCoord(goalCoord) - Vector3.one * 0.5f;

#if UNITY_EDITOR
        gridPlaneMeshR.sharedMaterial.SetVector("_HoleWorldPos", goalWorldPos);
        validGridSprite.sharedMaterial.SetVector("_HoleWorldPos", goalWorldPos);
#else
        gridPlaneMeshR.material.SetVector("_HoleWorldPos", goalWorldPos);
        validGridSprite.material.SetVector("_HoleWorldPos", goalWorldPos);
#endif

        goalBlockScript.transform.position = GetWorldSpaceFromCoord(goalCoord);
        goalBlockScript.SnapToGridWorldPos();
    }

    //[ReadOnly]
    //[SerializeReference]
    //[InlineEditor]


    public void SetGoalCoord(Vector2Int coord) {
        goalCoord = coord;
        EditorUtility.SetDirty(this);
    }

    public bool isValidGridCoord(Vector2Int coord) {
        var isValid = true;
        if (coord.x < 0 || coord.x >= gridSize.x) isValid = false;
        if (coord.y < 0 || coord.y >= gridSize.y) isValid = false;
        return isValid;
    }

    public BlockBehaviour QueryGridCoordBlockState(Vector2Int coord) {

        var isValid = true; // isValidGridCoord(coord);
        //Log($"print BlockState 2D Array size {ActiveGridState.GridBlockStates.GetLength(0)}, {ActiveGridState.GridBlockStates.GetLength(1)}");
        if (!isValid || ActiveGridState.GridBlockStates == null) return null;

        if (coord.x >= ActiveGridState.GridBlockStates.GetLength(0) || coord.y >= ActiveGridState.GridBlockStates.GetLength(1) || coord.x < 0 || coord.y < 0) {
            LogWarning($"tried to query invalid grid coord {coord}");
            return null;
        }


        return ActiveGridState.GridBlockStates[coord.x, coord.y];
    }

    public List<BlockBehaviour> QueryGridCoordForAllBlocks(Vector2Int coord) {
        List<BlockBehaviour> blocksFound = new List<BlockBehaviour>();
        foreach (var block in ActiveGridState.BlocksList) {
            if (block.coord == coord) {
                blocksFound.Add(block);
            }
        }
        return blocksFound;
    }

    public Vector3 GetWorldSpaceFromCoord(Vector2Int coord) {
        return GetBotLeftOriginPos() + (Vector3Int)coord + (Vector3)Vector2.one * .5f;
    }

    public Vector3 GetWorldPosSnappedToGrid(Vector3 pos) {
        var floatGridPos = pos - GetBotLeftOriginPos();
        var gridPos = new Vector2Int((int)floatGridPos.x, (int)floatGridPos.y);
        return GetWorldSpaceFromCoord(gridPos);
    }

    public Vector2Int GetGridCoordFromWorldPos(Vector3 pos) {
        var floatGridPos = pos - GetBotLeftOriginPos();
        floatGridPos -= new Vector3(0.5f, 0.5f, 0);
        var gridPos = new Vector2Int((int)floatGridPos.x, (int)floatGridPos.y);

        return gridPos;
    }

    public void TryPlaceOnGrid(BlockBehaviour block) {
        var pos = block.transform.position;
        var floatGridPos = pos - GetBotLeftOriginPos();
        var gridPos = new Vector2Int((int)floatGridPos.x, (int)floatGridPos.y);
        Log($"trying to add block {block.gameObject.name} to add block at {gridPos}");


        if (!isValidGridCoord(gridPos)) {
            //fail
            LogWarning($"failed to add block at {gridPos}, invalid coord");
            return;
        }

        if (QueryGridCoordBlockState(gridPos) != null) {
            //fail
            LogWarning($"failed to add block at {gridPos}, coord occupied");
            return;
        }

        //valid
        //try remove existing entry
        if (ActiveGridState.GridBlockStates == null)
            ActiveGridState.GridBlockStates = new BlockBehaviour[gridSize.x, gridSize.y];
        if (ActiveGridState.BlocksList.Remove(block))
            if (isValidGridCoord(block.coord))
                ActiveGridState.GridBlockStates[block.coord.x, block.coord.y] = null;

        //enter block into gridState
        ActiveGridState.GridBlockStates[gridPos.x, gridPos.y] = block;
        //snap to world pos
        block.transform.position = GetBotLeftOriginPos() + (Vector3)Vector2.one * .5f + (Vector3Int)gridPos;
        block.coord = gridPos;
        ActiveGridState.BlocksList.Add(block);
        ActiveGridState.UpdateCoordList();
        Log($"block added to grid at {gridPos}");

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public Vector3 GetTopLeftOriginPos() {
        return transform.position + new Vector3(-(float)gridSize.x * .5f, gridSize.y * .5f);
    }

    public Vector3 GetBotLeftOriginPos() {
        //float xAdd = gridSize.x % 2 == 0 ? 0f : .5f;
        //float yAdd = gridSize.y % 2 == 0 ? 0f : .5f;
        return transform.position + new Vector3(-(float)gridSize.x * .5f, -(float)gridSize.y * .5f);
    }

    public Vector3 GetTopLeftCellCenter() {
        return GetTopLeftOriginPos() + (Vector3.down + Vector3.right) * .5f;
    }

    public void ForEachCellAtCellCenter(Action<Vector2Int, Vector3> action) {
        for (var x = 0; x < gridSize.x; x++)
            for (var y = 0; y < gridSize.y; y++) {
                var cellCenterPos = GetBotLeftOriginPos() + Vector3.one * .5f + new Vector3(x, y);
                action(new Vector2Int(x, y), cellCenterPos);
            }
    }

    public void ForEachCellCoord(Action<Vector2Int> action) {
    }
}