using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System;
using UnityEditor;

public class BlockGrid : MonoBehaviour {

    [Title("Grid Settings")]
    [SerializeField, ReadOnly] private Vector2Int gridSize;
    public Vector2Int GridSize => gridSize;

    [SerializeField] private Vector2Int goalCoord;

    [SerializeField, InlineEditor]
    private LevelDataSO levelData;
    public LevelDataSO LevelData => levelData;

    [FoldoutGroup("Grid Rendering"), SerializeField]
    private SpriteRenderer validGridSprite;

    [FoldoutGroup("Grid Rendering"), SerializeField]
    private Transform gridPlaneParentT;

    [FoldoutGroup("Grid Rendering"), SerializeField]
    private MeshRenderer gridPlaneMeshR;

    [FoldoutGroup("Grid Rendering"), SerializeField]
    private SnapToGrid goalBlockScript;

    [FoldoutGroup("Actions"), Button(ButtonSizes.Medium)]
    public void LoadStateFromSO() {
        //destroy current blocks
        foreach (var b in ActiveGridState.BlocksList)
            GameObject.DestroyImmediate(b.gameObject);

        //clear active grid state
        ResetActiveGrid();

        //load data from level data SO
        gridSize = levelData.GridSize;
        goalCoord = levelData.GoalCoord;

        ResetActiveGrid();

        //load blocks from level data SO
        foreach(var bData in levelData.Blocks) {
            BlockBehaviour newBlock = GameObject.Instantiate(bData.blockTypeFab).GetComponent<BlockBehaviour>();
            newBlock.transform.position = GetWorldSpaceFromCoord(bData.gridCoord);
            newBlock.SetGridRef(this);
            newBlock.TryAddToGrid();

            newBlock.moveMode = bData.pathMode;
            newBlock.SetMovePath(bData.movePath?.ToArray());

            if (bData.startFrozen)
                newBlock.TrySetFreeze(true);

            newBlock.UpdateMovementVisualiser();
        }

        EditorUtility.SetDirty(this);
    }

    [FoldoutGroup("Actions"), Button(ButtonSizes.Medium)]
    public void SaveStateToSO() { }

    [FoldoutGroup("Actions"), Button(ButtonSizes.Large)]
    public void ResetActiveGrid() {
        gridSize = levelData.GridSize;
        ActiveGridState.GridBlockStates = new BlockBehaviour[levelData.GridSize.x, levelData.GridSize.y];
        ActiveGridState.BlocksList = new List<BlockBehaviour>();

        ReloadGridVisuals();
    }

    [FoldoutGroup("Actions"), Button(ButtonSizes.Medium)]
    public void ReloadGridVisuals() {
        validGridSprite.size = new Vector2(gridSize.x, gridSize.y);

        float planePosY = gridSize.y % 2 == 0 ? 0 : -0.5f / gridPlaneParentT.localScale.y;
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
    [SerializeField]
    public GridState ActiveGridState;


    public void SetGoalCoord(Vector2Int coord) {
        goalCoord = coord;
        EditorUtility.SetDirty(this);
    }
    public Vector2Int GoalCoord => goalCoord;

    private void Start() {
        //gridSize = startGridStateSO.GridSize;

        ActiveGridState.GridBlockStates = new BlockBehaviour[gridSize.x, gridSize.y];

        //foreach (var block in ActiveGridState.GridBlockStates) {
        //    if (block == null) continue;

        //    ActiveGridState.BlocksList.Add(block);
        //}

        ReloadGridVisuals();
    }

    public bool isValidGridCoord(Vector2Int coord) {
        bool isValid = true;
        if (coord.x < 0 || coord.x >= gridSize.x) isValid = false;
        if (coord.y < 0 || coord.y >= gridSize.y) isValid = false;
        return isValid;
    }

    public BlockBehaviour QueryGridCoordBlockState(Vector2Int coord) {
        var isValid = true; // isValidGridCoord(coord);
        //Debug.Log($"print BlockState 2D Array size {ActiveGridState.GridBlockStates.GetLength(0)}, {ActiveGridState.GridBlockStates.GetLength(1)}");
        if (!isValid || ActiveGridState.GridBlockStates == null) return null;
        else return ActiveGridState.GridBlockStates[coord.x, coord.y];
    }

    public Vector3 GetWorldSpaceFromCoord(Vector2Int coord) {
        return GetBotLeftOriginPos() + (Vector3Int)coord + (Vector3)Vector2.one * .5f;
    }

    public Vector3 GetWorldPosSnappedToGrid(Vector3 pos) {
        var floatGridPos = pos - GetBotLeftOriginPos();
        Vector2Int gridPos = new Vector2Int((int)floatGridPos.x, (int)floatGridPos.y);
        return GetWorldSpaceFromCoord(gridPos);
    }

    public Vector2Int GetGridCoordFromWorldPos(Vector3 pos) {
        var floatGridPos = pos - GetBotLeftOriginPos();
        Vector2Int gridPos = new Vector2Int((int)floatGridPos.x, (int)floatGridPos.y);
        return gridPos;
    }

    public void TryPlaceOnGrid(BlockBehaviour block) {
        var pos = block.transform.position;
        var floatGridPos = pos - GetBotLeftOriginPos();
        Vector2Int gridPos = new Vector2Int((int)floatGridPos.x, (int)floatGridPos.y);
        Debug.Log($"trying to add block {block.gameObject.name} to add block at {gridPos}");


        if (!isValidGridCoord(gridPos)) {
            //fail
            Debug.LogWarning($"failed to add block at {gridPos}, invalid coord");
            return;
        }
        if (QueryGridCoordBlockState(gridPos) != null) {
            //fail
            Debug.LogWarning($"failed to add block at {gridPos}, coord occupied");
            return;
        }

        //valid
        //try remove existing entry
        if (ActiveGridState.GridBlockStates == null) ActiveGridState.GridBlockStates = new BlockBehaviour[gridSize.x, gridSize.y];
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
        Debug.Log($"block added to grid at {gridPos}");

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    public Vector3 GetTopLeftOriginPos() {
        return transform.position + new Vector3(-(float)gridSize.x * .5f, (float)gridSize.y * .5f);
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
        for (int x = 0; x < gridSize.x; x++) {
            for (int y = 0; y < gridSize.y; y++) {
                Vector3 cellCenterPos = GetBotLeftOriginPos() + Vector3.one * .5f + new Vector3(x, y);
                action(new Vector2Int(x, y), cellCenterPos);
            }
        }
    }

    public void ForEachCellCoord(Action<Vector2Int> action) {

    }


#if UNITY_EDITOR
    private void OnDrawGizmos() {
        //Draw Debug grid lines
        Vector3 botLeft = GetBotLeftOriginPos();
        for (int i = 1; i < gridSize.x; i++) {
            Vector3 start = botLeft + Vector3.right * i;
            Gizmos.DrawLine(start, start + Vector3.up * gridSize.y);
        }
        for (int i = 1; i < gridSize.y; i++) {
            Vector3 start = botLeft + Vector3.up * i;
            Gizmos.DrawLine(start, start + Vector3.right * gridSize.y);
        }
    }
#endif
}
