using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[System.Serializable]
public class GridState
{
    private BlockState[,] gridBlockStates;
    public BlockState[,] GridBlockStates { 
        get { return gridBlockStates; } 
        set { 
            GridWidth = value.GetLength(0);
            GridHeight = value.GetLength(1);
            gridBlockStates = value;
        } 
    }

    [SerializeField]
    private List<BlockBehaviour> blocksList = new List<BlockBehaviour>();
    public List<BlockBehaviour> BlocksList {
        get { return blocksList; }
        set {
            blocksList = value;
            UpdateCoordList();
        }
    }

    [SerializeField]
    public Vector2Int[] BlockCoordList = new Vector2Int[0];
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

    public int GridWidth;
    public int GridHeight;

    public GridState(BlockState[,] bStates, List<BlockBehaviour> blockList)
    {
        GridBlockStates = bStates;
        BlocksList = blockList;
    }
}