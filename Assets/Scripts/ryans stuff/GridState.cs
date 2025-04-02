using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[System.Serializable]
public class GridState
{
    [SerializeField]
    private BlockBehaviour[,] gridBlockStates;
    public BlockBehaviour[,] GridBlockStates { 
        get { return gridBlockStates; } 
        set { 
            GridWidth = value.GetLength(0);
            GridHeight = value.GetLength(1);
            gridBlockStates = value;
        } 
    }

    [SerializeField]
    private List<BlockBehaviour> blocksList;
    public List<BlockBehaviour> BlocksList {
        get { return blocksList; }
        set {
            blocksList = value;
            UpdateCoordList();
        }
    }

    [SerializeField]
    public Vector2Int[] BlockCoordList;
    [Button]
    public void UpdateCoordList()
    {
        var coords = new List<Vector2Int>();
        foreach (BlockBehaviour b in blocksList) {
            coords.Add(b.coord);
        }
        BlockCoordList = coords.ToArray();
        //Debug.Log("this all happened");
    }

    [ReadOnly]
    public int GridWidth, GridHeight;

    public GridState(BlockBehaviour[,] bStates, List<BlockBehaviour> blockList)
    {
        GridBlockStates = bStates;
        BlocksList = blockList;
    }

    public void ClearBlockStateGrid()
    {
        gridBlockStates = new BlockBehaviour[GridWidth, GridHeight];
    }
}