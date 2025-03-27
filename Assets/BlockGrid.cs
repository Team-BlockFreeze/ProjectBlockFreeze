using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System;

public class BlockGrid : MonoBehaviour
{

    [SerializeField]
    private Vector2Int gridSize = new Vector2Int(5, 5);




    [Header("Grid State")]
    [SerializeField]
    private GridStateSO startGridState;
    public GridStateSO StartGridState => startGridState;

    [Button]
    public void LoadStateFromSO() {

    }
    [Button]
    public void SaveStateToSO() {

    }

    [Button]
    public void ResetActiveGrid()
    {
        ActiveGridState.GridBlockStates = new BlockState[startGridState.GridSize.x, startGridState.GridSize.y];
        ActiveGridState.BlocksList = new List<BlockBehaviour>();
    }

    [SerializeField]
    public GridState ActiveGridState = new GridState(new BlockState[5, 5], new List<BlockBehaviour>());

    public int testVar;

    public bool isValidGridCoord(Vector2Int coord)
    {
        bool isValid = true;
        if (coord.x < 0 || coord.x >= gridSize.x) isValid = false;
        if (coord.y < 0 || coord.y >= gridSize.y) isValid = false;
        return isValid;
    }

    public BlockState QueryGridValidCoordBlockState(Vector2Int coord)
    {
        return ActiveGridState.GridBlockStates[coord.x, coord.y];
        
        
    }

    public Vector3 GetWorldSpaceFromCoord(Vector2Int coord)
    {
        return GetBotLeftOriginPos() + (Vector3.right + Vector3.up) * .5f + (Vector3Int)coord;
    }

    public void TryPlaceOnGrid(BlockBehaviour block)
    {
        var pos = block.transform.position;
        var floatGridPos = pos - GetBotLeftOriginPos();
        Vector2Int gridPos = new Vector2Int((int)floatGridPos.x, (int)floatGridPos.y);


        if (!isValidGridCoord(gridPos) || QueryGridValidCoordBlockState(gridPos) != null) {
            //fail
            Debug.Log($"failed to add block at {gridPos}");
            return;
        }

        //valid
        //enter block into gridState
        ActiveGridState.GridBlockStates[gridPos.x, gridPos.y] = new BlockState();
        //snap to world pos
        block.transform.position = GetBotLeftOriginPos() + Vector3.one*.5f + (Vector3Int)gridPos;
        block.coord = gridPos;
        ActiveGridState.BlocksList.Add(block);
        ActiveGridState.UpdateCoordList();
        Debug.Log($"block added to grid at {gridPos}");
    }

    public Vector3 GetTopLeftOriginPos()
    {
        return transform.position + new Vector3(-(float)gridSize.x * .5f, (float)gridSize.y * .5f);
    }

    public Vector3 GetBotLeftOriginPos()
    {
        return transform.position + new Vector3(-(float)gridSize.x * .5f, -(float)gridSize.y * .5f);
    }

    public Vector3 GetTopLeftCellCenter()
    {
        return GetTopLeftOriginPos() + (Vector3.down + Vector3.right) * .5f;
    }

    public void ForEachCellAtCellCenter(Action<Vector2Int, Vector3> action)
    {
        for (int x = 0; x<gridSize.x; x++) {
            for(int y = 0; y<gridSize.y; y++) {
                Vector3 cellCenterPos = GetBotLeftOriginPos() + Vector3.one * .5f + new Vector3(x, y);
                action(new Vector2Int(x, y), cellCenterPos);
            }
        }
    }

    public void ForEachCellCoord(Action<Vector2Int> action)
    {

    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        //Draw Debug grid lines
        Vector3 botLeft = GetBotLeftOriginPos();
        for(int i = 1; i<gridSize.x; i++) {
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
