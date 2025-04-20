using Sirenix.OdinInspector;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GridStateSO", menuName = "ScriptableObjects/GridStateSO")]
[System.Serializable]
public class GridStateSO : ScriptableObject
{
    [BoxGroup("Grid Settings")]
    [SerializeField, MinValue(1)]
    private Vector2Int gridSize = new Vector2Int(5, 5);
    public Vector2Int GridSize => gridSize;

    [BoxGroup("Block States")]
    [ListDrawerSettings(ShowFoldout = true, DraggableItems = true)]
    [SerializeField]
    private List<BlockState> blocks = new List<BlockState>();
    public List<BlockState> Blocks => blocks;
}
